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
    public class EngineeringManagerSystem : BaseSearchSystem
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
                ComponentType.Create<EngineeringManager.Component>(),
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
            var engineeringManager = group.GetComponentDataArray<EngineeringManager.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < engineeringManager.Length; i++) {
                var manager = engineeringManager[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Stronghold)
                    continue;

                if (manager.FreeEngineers.Count == 0)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = manager.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                manager.Interval = inter;

                var deadList = new List<EntityId>();

                foreach(var kvp in manager.EngineeringPoints) {
                    BaseUnitStatus.Component? comp = null;
                    if (TryGetComponent(kvp.Key, out comp) == false)
                        continue;

                    if (comp.Value.Type != UnitType.Stronghold || comp.Value.State != UnitState.Alive)
                        continue;

                    deadList.Add(kvp.Key);
                }

                var emptyList = manager.FreeEngineers;

                while(emptyList.Count > 0) {
                    var entity = emptyList[0];
                    if (MakeEngineeringPlan(entity, deadList, ref manager) == 0)
                        break;
                    emptyList.RemoveAt(0);
                }

                engineeringManager[i] = manager;
            }
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
