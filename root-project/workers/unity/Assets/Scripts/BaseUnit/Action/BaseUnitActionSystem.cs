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
        private double checkedTime = -1.0;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitAction.Component>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<GunComponent.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<PostureBoneContainer>(),
                ComponentType.ReadOnly<CombinedAimTracer>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitAction.Component action,
                                          ref GunComponent.Component gun,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (target.State != TargetState.ActionTarget)
                    return;

                var current = Time.ElapsedTime;
                var diff = checkedTime <= 0 ? 0: current - checkedTime;
                checkedTime = current;

                Vector3? epos = null;
                if (action.EnemyPositions.Count > 0)
                    epos = action.EnemyPositions[0].ToWorkerPosition(this.Origin);

                var container = EntityManager.GetComponentObject<PostureBoneContainer>(entity);
                var tracer = EntityManager.GetComponentObject<CombinedAimTracer>(entity);
                Attack(container, tracer, current, diff, epos, entityId, ref gun);
            });
        }

        void Attack(PostureBoneContainer container, CombinedAimTracer tracer, double current, double diff, Vector3? epos, in SpatialEntityId entityId, ref GunComponent.Component gun)
        {
            var gunsDic = gun.GunsDic;
            var updGuns = false;

            if (tracer != null)
            {
                tracer.SetAimTarget(epos);
                tracer.Rotate(diff);
            }

            if (epos == null)
                return;

            if (container == null || container.Bones == null)
                return;

            foreach (var bone in container.Bones)
            {
                GunInfo gunInfo;
                if (gunsDic.TryGetValue(bone.hash, out gunInfo) == false)
                    continue;

                var result = CheckRange(container.GetCannon(bone.hash), epos.Value, gunInfo.AttackRange, gunInfo.AttackAngle);
                switch (result)
                {
                    case Result.InRange:
                        if (gunInfo.StockBullets == 0)
                            break;
                        var inter = gunInfo.Interval;
                        if (inter.CheckTime(current) == false)
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

        Result CheckRange(CannonTransform cannon, in Vector3 epos, float range, float angle)
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
