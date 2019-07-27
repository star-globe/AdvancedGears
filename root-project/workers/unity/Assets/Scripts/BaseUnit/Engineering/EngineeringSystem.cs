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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class EngineeringSystem : BaseSearchSystem
    {
        EntityQuery group;
        CommandSystem commandSystem;
        ComponentUpdateSystem updateSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingSystem<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingSystem<CommandSystem>();
            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            group = GetEntityQuery(
                ComponentType.ReadWrite<EngineeringComponent.Component>(),
                ComponentType.ReadWrite<FuelComponent.Component>(),
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
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref EngineeringComponent.Component engineer,
                                          ref FuelComponent.Component fuel,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Engineer)
                    return;

                if (engineer.OrderFinished)
                    return;

                var time = Time.time;
                var inter = engineer.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                engineer.Interval = inter;

                float range = engineer.Range;
                var f_comp = fuel;

                var id = engineer.Order.Point.UnitId;
                bool isEnemy = false;
                bool allowDead = false;
                switch (engineer.Order.Type)
                {
                    case EngineeringType.Repair: isEnemy = false; allowDead = true; break;
                    case EngineeringType.Occupy: isEnemy = true; allowDead = true; break;
                }

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var unit = getUnits(status.Side, pos, range, isEnemy, allowDead, UnitType.Stronghold).FirstOrDefault(u => u.id == id);
                if (unit != null)
                {
                    bool tof = DealOrder(unit, engineer.Order.Type, status.Side, ref f_comp);
                    SendResult(tof, engineer.ManagerId, entityId.EntityId, engineer.Order);
                    engineer.OrderFinished = true;
                }

                if (fuel.Fuel != f_comp.Fuel)
                    fuel = f_comp;
            });
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
                EngineeringComponent.Component? comp = null; 
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

                    fuel.Fuel -= repairCost;
                    break;

                default:
                    return false;
            }

            Unity.Entities.Entity entity;
            if (TryGetEntity(unit.id, out entity) == false)
                return false;

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
