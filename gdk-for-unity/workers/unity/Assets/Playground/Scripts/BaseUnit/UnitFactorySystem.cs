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
        private struct Data
        {
            public readonly int Length;
            public ComponentDataArray<UnitFactory.Component> Factory;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            [ReadOnly] public ComponentArray<Transform> Transform;
            [ReadOnly] public ComponentDataArray<SpatialEntityId> EntityId;
        }

        [Inject] private Data data;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var factory = data.Factory[i];
                var status = data.Status[i];
                var pos = data.Transform[i].position;
                var entityId = data.EntityId[i];

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

                data.Factory[i] = factory;
                //data.CommanderStatus[i] = commander;
            }
        }
    }
}
