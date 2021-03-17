using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FuelSupplySystem : BaseSearchSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<FuelSupplyer.Component>(),
                ComponentType.ReadWrite<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            HandleRequest();
            HandleResponse();
        }

        void HandleRequest()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref FuelSupplyer.Component supply,
                                          ref FuelComponent.Component fuel,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Supply)
                    return;

                if (supply.OrderFinished)
                    return;

                var inter = supply.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                supply.Interval = inter;

                float range = supply.Range;
                var f_comp = fuel;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var id = supply.Order.Point.StrongholdId;
                var units = getAllyUnits(status.Side, pos, range, allowDead: false, GetSingleUnitTypes(UnitType.Stronghold));
                foreach (var unit in units)
                {
                    if (unit.id != id)
                        continue;

                    FuelModifyType type = FuelModifyType.None;
                    switch (supply.Order.Type)
                    {
                        case SupplyOrderType.Deliver: type = FuelModifyType.Feed; break;
                        case SupplyOrderType.Accept: type = FuelModifyType.Absorb; break;
                    }

                    bool tof = DealOrder(unit, type, ref f_comp);
                    SendResult(tof, supply.ManagerId, entityId.EntityId, supply.Order);
                    supply.OrderFinished = true;

                    break;
                }

                if (fuel.Fuel != f_comp.Fuel)
                    fuel = f_comp;
            });
        }

        void HandleResponse()
        {
            var responses = this.CommandSystem.GetResponses<FuelSupplyManager.FinishOrder.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                var response = responses[i];
                if (response.StatusCode != StatusCode.Success) {
                    // Handle command failure
                    continue;
                }

                var order = response.ResponsePayload;
                if (order.Value.Type == SupplyOrderType.None)
                    continue;

                var entity = response.SendingEntity;
                FuelSupplyer.Component? comp = null; 
                if (TryGetComponent(entity, out comp) == false)
                    continue;
                
                var supplyer = comp.Value;
                supplyer.Order = order.Value;
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
            this.UpdateSystem.SendEvent(new FuelComponent.FuelModified.Event(modify), unit.id);
            return true;


        }

        void SendResult(bool success, EntityId manager_id, EntityId self_id,  in SupplyOrder order)
        {
            Unity.Entities.Entity entity;
            if (TryGetEntity(manager_id, out entity) == false)
                return;

            var res = new SupplyOrderResult { Result = success, SelfId = self_id, Order = order};
            this.CommandSystem.SendCommand(new FuelSupplyManager.FinishOrder.Request(manager_id, res), entity);
        }
    }
}
