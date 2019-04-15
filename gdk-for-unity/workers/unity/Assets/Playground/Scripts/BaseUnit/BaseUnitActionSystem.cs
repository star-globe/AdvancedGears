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
    internal class BaseUnitActionSystem : ComponentSystem
    {
        private struct Data
        {
            public readonly int Length;
            public ComponentDataArray<BaseUnitAction.Component> Action;
            public ComponentDataArray<BaseUnitAction.EventSender.FireTriggered> FireTriggeredEventsSenders;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
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
                var action = data.Action[i];
                var triggerSender = data.FireTriggeredEventsSenders[i];
                var status = data.Status[i];
                var trans = data.Transform[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (!action.IsTarget)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = action.Interval;
                if (time - action.LastActed < inter)
                    continue;

                action.LastActed = time + RandomInterval.GetRandom(inter);

                if (action.EnemyPositions.Count > 0)
                {
                    var epos = action.EnemyPositions[0].ToUnityVector() + origin;
                    if (CheckRange(trans, epos, action.AttackRange, action.AttackAngle))
                    {
                        var info = new AttackTargetInfo
                        {
                            Type = 1,
                            TargetPosition = action.EnemyPositions[0],
                        };

                        triggerSender.Events.Add(info);
                    }
                }

                data.Action[i] = action;
            }
        }

        bool CheckRange(Transform trans, Vector3 epos, float range, float angle)
        {
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return false;

            var dot = Vector3.Dot(diff.normalized, trans.forward);
            return dot > Mathf.Cos(angle);
        }
    }
}
