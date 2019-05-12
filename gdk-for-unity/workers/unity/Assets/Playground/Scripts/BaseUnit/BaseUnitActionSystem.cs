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
            public ComponentDataArray<BaseUnitPosture.Component> Posture;
            public ComponentDataArray<BaseUnitPosture.EventSender.PostureChanged> PostureChangedEventSenders;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            public ComponentArray<Transform> Transform;
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
                var posture = data.Posture[i];
                var postureSender = data.PostureChangedEventSenders[i];
                var status = data.Status[i];
                var trans = data.Transform[i];

                var unit = trans.GetComponent<UnitTransform>();
                if (unit == null)
                    continue;

                if (status.State != UnitState.Alive)
                    continue;

                if (!action.IsTarget)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = action.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                action.Interval = inter;

                if (action.EnemyPositions.Count > 0)
                {
                    var epos = action.EnemyPositions[0].ToUnityVector() + origin;
                    var result = CheckRange(unit, epos, action.AttackRange, action.AttackAngle, action.AngleSpeed);
                    switch (result)
                    {
                        case Result.InRange:
                            var atk = new AttackTargetInfo
                            {
                                Type = 1,
                                TargetPosition = action.EnemyPositions[0],
                            };
                            triggerSender.Events.Add(atk);
                            break;

                        case Result.Rotate:
                            var rot = unit.Cannon.Turret.rotation;
                            var pdata = new PostureData(PosturePoint.Bust, new Improbable.Transform.Quaternion(rot.w, rot.x, rot.y, rot.z));
                            postureSender.Events.Add(pdata);
                            var pos = posture.Posture;
                            pos.SetData(pdata);
                            posture.Posture = pos;
                            data.Posture[i] = posture;
                            break;
                    }
                }

                data.Action[i] = action;
            }
        }

        enum Result
        {
            OutOfRange = 0,
            InRange,
            Rotate,
        }

        Result CheckRange(UnitTransform unit, Vector3 epos, float range, float angle, float angleSpeed)
        {
            var trans = unit.Cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            var foward = diff.normalized;
            var up = unit.Conntector.up;
            if (RotateLogic.CheckRotate(unit.Cannon.Turret, up, foward, angle))
                return Result.InRange;

            RotateLogic.Rotate(unit.Cannon.Turret, up, foward, angleSpeed * Time.deltaTime);
            return Result.Rotate;
        }
    }
}
