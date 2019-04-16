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
    public class CommanderUnitSearchSystem : BaseSearchSystem
    {
        private struct Data
        {
            public readonly int Length;
            public ComponentDataArray<CommanderSight.Component> Sight;
            public ComponentDataArray<CommanderStatus.Component> CommanderStatus;
            public ComponentDataArray<BaseUnitStatus.Component> BaseUnitStatus;
            [ReadOnly] public ComponentArray<Transform> Transform;
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
                var sight = data.Sight[i];
                var commander = data.CommanderStatus[i];
                var status = data.BaseUnitStatus[i];
                var pos = data.Transform[i].position;

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = sight.Interval;
                if (time - sight.LastSearched < inter)
                    continue;

                sight.LastSearched = time + RandomInterval.GetRandom(inter);

                var tgt = getNearestEnemeyPosition(status.Side, pos, sight.Range, UnitType.Stronghold);
                sight.IsTarget = tgt != null;
                if (sight.IsTarget)
                {
                    var tpos = new Improbable.Vector3f(tgt.Value.x - origin.x,
                                                       tgt.Value.y - origin.y,
                                                       tgt.Value.z - origin.z);
                    sight.TargetPosition = pos;
                }

                data.CommanderStatus[i] = sight;
            }
        }
    }
}
