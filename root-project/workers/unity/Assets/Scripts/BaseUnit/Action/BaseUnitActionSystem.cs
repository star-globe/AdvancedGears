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
        private EntityQueryBuilder.F_EDDDDDD<BaseUnitAction.Component, GunComponent.Component, PostureAnimation.Component, BaseUnitTarget.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitAction.Component>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<GunComponent.HasAuthority>(),
                ComponentType.ReadWrite<PostureAnimation.Component>(),
                ComponentType.ReadOnly<PostureAnimation.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<PostureBoneContainer>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach(action);
        } 
            
        private void Query (Entity entity,
                            ref BaseUnitAction.Component action,
                            ref GunComponent.Component gun,
                            ref PostureAnimation.Component anim,
                            ref BaseUnitTarget.Component target,
                            ref BaseUnitStatus.Component status,
                            ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (UnitUtils.IsOffensive(status.Type) == false)
                return;

            if (target.State != TargetState.ActionTarget)
                return;

            var current = Time.ElapsedTime;

            Vector3? epos = null;
            if (action.EnemyPositions.Count > 0) {
                epos = action.EnemyPositions[0].ToWorkerPosition(this.Origin);

                var container = EntityManager.GetComponentObject<PostureBoneContainer>(entity);
                Attack(container, current, epos.Value, entityId, ref gun);
            }

            var type = AnimTargetType.None;
            bool isDiff = false;
            if (epos != null)
            {
                isDiff = anim.AnimTarget.Position.ToWorkerPosition(this.Origin) != epos.Value;
                type = AnimTargetType.Position;
            }

            if (anim.AnimTarget.Type != type || isDiff)
            {
                var animTarget = anim.AnimTarget;
                animTarget.Type = type;

                if (epos != null)
                    animTarget.Position = epos.Value.ToWorldPosition(this.Origin);

                anim.AnimTarget = animTarget;
            }
        }

        void Attack(PostureBoneContainer container, double current, in Vector3 epos, in SpatialEntityId entityId, ref GunComponent.Component gun)
        {
            var gunsDic = gun.GunsDic;
            var updGuns = false;

            if (container == null || container.Bones == null)
                return;

            foreach (var bone in container.Bones)
            {
                GunInfo gunInfo;
                if (gunsDic.TryGetValue(bone.hash, out gunInfo) == false)
                    continue;

                var result = CheckRange(container.GetCannon(bone.hash), epos, gunInfo.AttackRange, gunInfo.AttackAngle);
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
