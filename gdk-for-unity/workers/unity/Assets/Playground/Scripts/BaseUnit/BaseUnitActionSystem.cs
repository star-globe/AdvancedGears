using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
                    var pos = posture.Posture;
                    bool tof = false;

                    foreach (var k in unit.GetKeys())
                    {
                        List<Improbable.Transform.Quaternion> list = null;
                        SetPosture(entityId.EntityId, unit, k, epos, action.AttackRange, action.AttackAngle, action.AngleSpeed, out list);

                        if (list != null)
                        {
                            pos.SetData(new PostureData(k, list));
                            tof |= true;
                        }
                    }

                    if (tof)
                    {
                        posture.Posture = pos;
                        postureData[i] = posture;
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

        void SetPosture(EntityId entityId, UnitTransform unit, PosturePoint point, Vector3 epos, float attackRange, float attackAngle, float angleSpeed, out List<Improbable.Transform.Quaternion> list)
        {
            list = null;

            var postrans = unit.GetPosture(point);
            var cannon = unit.GetCannonTransform(point);
            var result = CheckRange(postrans, cannon, epos, attackRange, attackAngle, angleSpeed);
            switch (result)
            {
                case Result.InRange:
                    var atk = new AttackTargetInfo
                    {
                        Type = 1,
                        TargetPosition = epos.ToImprobableVector3(),
                        Attached = point,
                    };
                    updateSystem.SendEvent(new GunComponent.FireTriggered.Event(atk), entityId);
                    break;

                case Result.Rotate:
                    var rot = unit.transform.rotation;
                    list = new List<Improbable.Transform.Quaternion>(postrans.GetQuaternions().Select(q => q.ToImprobableQuaternion()));
                    var pdata = new PostureData(point, list);
                    updateSystem.SendEvent(new BaseUnitPosture.PostureChanged.Event(pdata), entityId);
                    break;
            }
        }


        Result CheckRange(PostureTransform posture, CannonTransform cannon, Vector3 epos, float range, float angle, float angleSpeed)
        {
            var trans = cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            var foward = diff.normalized;
            if (RotateLogic.CheckRotate(cannon.Forward, cannon.HingeAxis, foward, angle))
                return Result.InRange;
            
            posture.Resolve(epos, cannon.Muzzle, angleSpeed * Time.deltaTime);
            return Result.Rotate;
        }
    }
}
