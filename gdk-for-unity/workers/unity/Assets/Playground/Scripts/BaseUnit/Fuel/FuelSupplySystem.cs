using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    public class FuelSupplySystem : BaseSearchSystem
    {
        ComponentGroup group;
        CommandSystem commandSystem;
        ComponentUpdateSystem updateSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingManager<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingManager<CommandSystem>();
            updateSystem = World.GetExistingManager<ComponentUpdateSystem>();
            group = GetComponentGroup(
                ComponentType.Create<FuelSupplyer.Component>(),
                ComponentType.Create<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(FuelSupplyer.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleRequest();
            HandleResponse();
        }

        void HandleRequest()
        {
            var fuelSupplyer = group.GetComponentDataArray<FuelSupplyer.Component>();
            var fuelData = group.GetComponentDataArray<FuelComponent.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var targetData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < fuelSupplyer.Length; i++) {
                var supply = fuelSupplyer[i];
                var fuel = fuelData[i];
                var status = statusData[i];
                var tgt = targetData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Supply)
                    continue;

                if (supply.OrderFinished)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = supply.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                supply.Interval = inter;

                float range = supply.Range;
                var f_comp = fuel.Fuel;

                var id = supply.Order.Point.StrongholdId;
                var unit = getUnits(status.Side, pos, range, false, false, UnitType.Stronghold).FirstOrDefault(u => u.id == id);
                if (unit != null) {
                    FuelModifyType type = FuelModifyType.None;
                    switch (supply.Order.Type) {
                        case SupplyOrderType.Deliver: type = FuelModifyType.Feed; break;
                        case SupplyOrderType.Accept:  type = FuelModifyType.Absorb; break;
                    }

                    bool tof = DealOrder(unit, type, ref f_comp);
                    SendResult(tof, supply.ManagerId, entityId, supply.Order);
                    supply.OrderFinished = true;
                }

                if (fuel.Fuel != f_comp.Fuel)
                    fuelData[i] = f_comp;
                
                fuelSupplyer[i] = supply;
            }
        }

        void HandleResponse()
        {
            var responses = commandSystem.GetResponses<FuelSupplyManager.FinishOrder.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                var response = responses[i];
                if (response.StatusCode != StatusCode.Success) {
                    // Handle command failure
                    continue;
                }

                var order = response.ResponsePayload;
                if (order.Type == SupplyOrderType.None)
                    continue;

                var entity = response.SendingEntity;
                FuelSupplyer.Component? comp = null; 
                if (TryGetComponent(entity, out comp) == false)
                    continue;
                
                var supplyer = comp.Value;
                supplyer.Order = order;
                supplyer.OrderFinished = false;

                SetComponent(entity, supplyer);
            }
        }

        bool DealOrder(UnitInfo unit, FuelModifyType type, ref FuelComponent.Component fuel)
        {
            FuelComponent.Component? comp = null;
            if (TryGetComponent(unit.id, out comp) == false)
                return false;

            var t_fuel = comp.Value;

            int num = 0;
            switch(type) {
                case FuelModifyType.Feed:
                    num = Mathf.Clamp(t_fuel.EmptyCapacity(), 0, fuel.Fuel);
                    fuel.Fuel -= num;
                    break;

                case FuelModifyType.Absorb:
                    num = Mathf.Clamp(fuel.EmptyCapacity(), 0 , t_fuel.Fuel);
                    fuel.Fuel += num;
                    break;

                default:
                    return false;
            }

            var modify = new FuelModifier {
                Type = type,
                Amount = num,
            };
            updateSystem.SendEvent(new FuelComponent.FuelModified.Event(modify), unit.id);
            return true;


        }

        void SendResult(bool success, EntityId manager_id, EntityId self_id,  in SupplyOrder order)
        {
            Entity entity;
            if (TryGetEntity(manager_id, out entity) == false)
                return;

            var res = new OrderResult { Result = success, SelfId = self_id, Order = order};
            commandSystem.SendCommand(new FuelSupplyManager.FinishOrder.Request(manager_id, res), entity);
        }
    }
}
