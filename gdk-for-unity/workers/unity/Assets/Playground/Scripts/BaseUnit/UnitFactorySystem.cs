using System;
using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    public class UnitFactorySystem : ComponentSystem
    {
        ComponentGroup group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;

            group = GetComponentGroup(
                ComponentType.Create<UnitFactory.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            var factoryData = group.GetComponentDataArray<UnitFactory.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < factoryData.Length; i++)
            {
                var factory = factoryData[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = factory.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                factory.Interval = inter;
                /*
                var tgt = getNearestEnemeyPosition(status.Side, pos, sight.Range, UnitType.Stronghold);
                sight.IsTarget = tgt != null;
                var tpos = Improbable.Vector3f.Zero;
                if (sight.IsTarget)
                {
                    tpos = new Improbable.Vector3f(tgt.Value.x - origin.x,
                                                   tgt.Value.y - origin.y,
                                                   tgt.Value.z - origin.z);
                }

                sight.TargetPosition = tpos;

                // check 
                OrderType current = GetOrder(status.Side, pos, sight.Range);

                bool isOrderChanged = commander.SelfOrder != current;
                commander.SelfOrder = current;

                SetFollowers(commander.Followers,
                             new TargetInfo (sight.IsTarget,
                                             sight.TargetPosition,
                                             entityId.EntityId,
                                             commander.AllyRange),
                             isOrderChanged,
                             current);
                */

                factoryData[i] = factory;
                //data.CommanderStatus[i] = commander;
            }
        }
    }
}
