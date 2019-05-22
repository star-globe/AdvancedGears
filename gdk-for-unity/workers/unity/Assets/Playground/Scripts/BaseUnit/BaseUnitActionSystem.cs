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
        private ComponentUpdateSystem updateSystem;
        private ComponentGroup group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            updateSystem = World.GetExistingManager<ComponentUpdateSystem>();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;

            group = GetComponentGroup(
                ComponentType.Create<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.Create<BaseUnitPosture.Component>(),
                ComponentType.ReadOnly<BaseUnitPosture.ComponentAuthority>(),
                ComponentType.Create<GunComponent.Component>(),
                ComponentType.Create<UnitTransform>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
            group.SetFilter(BaseUnitPosture.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var actionData = group.GetComponentDataArray<BaseUnitAction.Component>();
            var postureData = group.GetComponentDataArray<BaseUnitPosture.Component>();
            var gunData = group.GetComponentDataArray<GunComponent.Component>();
            var unitData = group.GetComponentArray<UnitTransform>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < actionData.Length; i++)
            {
                var action = actionData[i];
                var posture = postureData[i];
                var gun = gunData[i];
                var status = statusData[i];
                var unit = unitData[i];
                var entityId = entityIdData[i];

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
                    var result = CheckRange(unit.GetTerminal<CannonTransform>(PosturePoint.Bust), epos, action.AttackRange, action.AttackAngle, action.AngleSpeed);
                    switch (result)
                    {
                        case Result.InRange:
                            var atk = new AttackTargetInfo
                            {
                                Type = 1,
                                TargetPosition = action.EnemyPositions[0],
                            };
                            updateSystem.SendEvent(new BaseUnitAction.FireTriggered.Event(atk), entityId.EntityId);
                            break;

                        case Result.Rotate:
                            var rot = unit.transform.rotation;
                            var list = new List<Improbable.Transform.Quaternion>();
                            var pdata = new PostureData(PosturePoint.Bust, list);//new Improbable.Transform.Quaternion(rot.w, rot.x, rot.y, rot.z));
                            updateSystem.SendEvent(new BaseUnitPosture.PostureChanged.Event(pdata), entityId.EntityId);
                            var pos = posture.Posture;
                            pos.SetData(pdata);
                            posture.Posture = pos;
                            postureData[i] = posture;
                            break;
                    }
                }

                actionData[i] = action;
            }
        }

        enum Result
        {
            OutOfRange = 0,
            InRange,
            Rotate,
        }

        Result CheckRange(CannonTransform cannon, Vector3 epos, float range, float angle, float angleSpeed)
        {
            var trans = cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            //var foward = diff.normalized;
            //var up = unit.Conntector.up;
            //if (RotateLogic.CheckRotate(unit.Cannon.Turret, up, foward, angle))
            //    return Result.InRange;
            //
            //RotateLogic.Rotate(unit.Cannon.Turret, up, foward, angleSpeed * Time.deltaTime);
            return Result.Rotate;
        }
    }
}
