using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitActionSystem : SpatialComponentSystem
    {
        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.HasAuthority>(),
                ComponentType.ReadWrite<BaseUnitPosture.Component>(),
                ComponentType.ReadOnly<BaseUnitPosture.HasAuthority>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<UnitTransform>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitAction.Component action,
                                          ref BaseUnitPosture.Component posture,
                                          ref GunComponent.Component gun,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (target.State != TargetState.ActionTarget)
                    return;

                var time = Time.ElapsedTime;
                var inter = action.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                action.Interval = inter;

                if (action.EnemyPositions.Count > 0)
                {
                    var epos = action.EnemyPositions[0].ToWorkerPosition(this.Origin);
                    var container = EntityManager.GetComponentObject<PostureBoneContainer>(entity);
                    Attack(container, time, action.AngleSpeed, epos, entityId, ref gun);

                    //if (updPosture)
                    //    postureData[i] = posture;

                    //if (updGuns)
                    //    gunData[i] = gun;
                }
            });
        }

        void Attack(PostureBoneContainer container, double time, float angleSpeed, in Vector3 epos, in SpatialEntityId entityId, ref GunComponent.Component gun)
        {
            var gunsDic = gun.GunsDic;
            var updGuns = false;

            if (container.Bones == null)
                return;

            foreach (var bone in container.Bones)
            {
                GunInfo gunInfo;
                if (gunsDic.TryGetValue(bone.hash, out gunInfo) == false)
                    continue;

                var result = CheckRange(container.GetCannon(bone.hash), epos, gunInfo.AttackRange, gunInfo.AttackAngle, angleSpeed);
                switch (result)
                {
                    case Result.InRange:
                        if (gunInfo.StockBullets == 0)
                            break;
                        var inter = gunInfo.Interval;
                        if (inter.CheckTime(time) == false)
                            break;
                        gunInfo.Interval = inter;
                        var atk = new AttackTargetInfo
                        {
                            GunTypeId = gunInfo.GunTypeId,
                            TargetPosition = epos.ToFixedPointVector3(),
                            AttachedBone = bone.hash,
                        };
                        updGuns |= true;
                        this.UpdateSystem.SendEvent(new GunComponent.FireTriggered.Event(atk), entityId.EntityId);
                        break;
                    case Result.Rotate:
                        break;
                }
            }


            if (updGuns)
                gun.GunsDic = gunsDic;
        }

        enum Result
        {
            OutOfRange = 0,
            InRange,
            Rotate,
        }

        Result CheckRange(CannonTransform cannon, in Vector3 epos, float range, float angle, float angleSpeed)
        {
            var trans = cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            var foward = diff.normalized;
            if (Vector3.Angle(cannon.Forward, foward) < angle)
                return Result.InRange;

            return Result.Rotate;
        }
    }
}
