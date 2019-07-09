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
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class EngineeringManagerSystem : BaseSearchSystem
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
                ComponentType.ReadWrite<EngineeringManager.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(EngineeringManager.ComponentAuthority.Authoritative);
        }

        const float checkRateUpper = 0.7f;
        const float checkRateUnder = 0.4f;

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((ref EngineeringManager.Component manager,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (manager.FreeEngineers.Count == 0)
                    return;

                var time = Time.time;
                var inter = manager.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                manager.Interval = inter;

                var deadList = new List<EntityId>();

                foreach (var kvp in manager.EngineeringPoints)
                {
                    BaseUnitStatus.Component? comp = null;
                    if (TryGetComponent(kvp.Key, out comp) == false)
                        continue;

                    if (comp.Value.Type != UnitType.Stronghold || comp.Value.State != UnitState.Alive)
                        continue;

                    deadList.Add(kvp.Key);
                }

                var emptyList = manager.FreeEngineers;
                while (emptyList.Count > 0)
                {
                    var entity = emptyList[0];
                    if (MakeEngineeringPlan(entity, deadList, ref manager) == 0)
                        break;
                    emptyList.RemoveAt(0);
                }
            });
        }

        int MakeEngineeringPlan(EntityId entityId, List<EntityId> deadList, ref EngineeringManager.Component manager)
        {
            EngineeringComponent.Component? comp = null;
            if (TryGetComponent(entityId, out comp) == false)
                return -1;

            var map = manager.EngineeringPoints;
            var plan = new EngineeringPlan { Orders = new List<EngineeringOrder>() };

            if (plan.Orders.Count == 0)
                return 0;

            Unity.Entities.Entity entity;
            if (TryGetEntity(entityId, out entity) == false)
                return -1;

            commandSystem.SendCommand(new EngineeringComponent.SetOrder.Request(entityId, plan.Orders[0]), entity);

            TargetInfo tgt;
            MakeTarget(plan.Orders[0], out tgt);
            commandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(entityId, tgt), entity);

            manager.EngineeringOrders.Add(entityId, plan);
            return 1;
        }

        void MakeTarget(in EngineeringOrder order, out TargetInfo targetInfo)
        {
            targetInfo = new TargetInfo 
            {
                IsTarget = true,
                TargetId = order.Point.UnitId,
                Position = order.Point.Pos,
                Type = order.Point.UnitType,
                Side = order.Point.Side,
                CommanderId = new EntityId(-1),
                AllyRange = 0.0f,
            };
        }

    }
}
