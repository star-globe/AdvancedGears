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
    public class EngineeringSystem : BaseSearchSystem
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
                ComponentType.Create<EngineeringComponent.Component>(),
                ComponentType.Create<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(EngineeringComponent.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleRequest();
            HandleResponse();
        }

        void HandleRequest()
        {
            var engineeringData = group.GetComponentDataArray<EngineeringComponent.Component>();
            var fuelData = group.GetComponentDataArray<FuelComponent.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var targetData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < engineeringData.Length; i++) {
                var engineer = engineeringData[i];
                var fuel = fuelData[i];
                var status = statusData[i];
                var tgt = targetData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Engineer)
                    continue;

                if (engineer.OrderFinished)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = engineer.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                engineer.Interval = inter;

                float range = engineer.Range;
                var f_comp = fuel;

                var id = engineer.Order.Point.UnitId;
                bool isEnemy = false;
                bool allowDead = false;
                switch (engineer.Order.Type) {
                    case EngineeringType.Repair: isEnemy = false; allowDead = true; break;
                    case EngineeringType.Occupy: isEnemy = true; allowDead = true; break;
                }

                var unit = getUnits(status.Side, pos, range, isEnemy, allowDead, UnitType.Stronghold).FirstOrDefault(u => u.id == id);
                if (unit != null) {
                    bool tof = DealOrder(unit, type, status.Side, ref f_comp);
                    SendResult(tof, engineer.ManagerId, entityId.EntityId, engineer.Order);
                    engineer.OrderFinished = true;
                }

                if (fuel.Fuel != f_comp.Fuel)
                    fuelData[i] = f_comp;
                
                engineeringData[i] = engineer;
            }
        }

        void HandleResponse()
        {
            var responses = commandSystem.GetResponses<EngineeringManager.FinishOrder.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                var response = responses[i];
                if (response.StatusCode != StatusCode.Success) {
                    // Handle command failure
                    continue;
                }

                var order = response.ResponsePayload;
                if (order.Value.Type == EngineeringType.None)
                    continue;

                var entity = response.SendingEntity;
                Engineering.Component? comp = null; 
                if (TryGetComponent(entity, out comp) == false)
                    continue;
                
                var engineer = comp.Value;
                engineer.Order = order.Value;
                engineer.OrderFinished = false;

                SetComponent(entity, engineer);
            }
        }

        const int repairCost = 100;
        bool DealOrder(UnitInfo unit, EngineeringType type, UnitSide selfSide, ref FuelComponent.Component fuel)
        {
            BaseUnitStatus.Component? comp = null;
            if (TryGetComponent(unit.id, out comp) == false)
                return false;

            var state = comp.Value.State;
            switch(type) {
                case EngineeringType.Repair:
                case EngineeringType.Occupy:
                    if (state != UnitState.Dead)
                        return false;

                    if (fuel.Fuel < repairCost)
                        return false;

                    fuel.Fuel -= num;
                    break;

                default:
                    return false;
            }

            Unity.Entities.Entity entity;
            if (TryGetEntity(unit.id, out entity) == false)
                return;

            var req = new ForceStateChange { Side = selfSide, State = UnitState.Alive };
            commandSystem.SendCommand(new BaseUnitStatus.ForceState.Request(unit.id, req), entity);
            return true;


        }

        void SendResult(bool success, EntityId manager_id, EntityId self_id,  in EngineeringOrder order)
        {
            Unity.Entities.Entity entity;
            if (TryGetEntity(manager_id, out entity) == false)
                return;

            var res = new EngineeringOrderResult { Result = success, SelfId = self_id, Order = order};
            commandSystem.SendCommand(new EngineeringManager.FinishOrder.Request(manager_id, res), entity);
        }
    }
}
