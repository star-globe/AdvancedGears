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
        private EntityQuerySet querySet;
        private EntityQueryBuilder.F_EDDDDDD<UnitActionData, GunComponent.Component, PostureAnimation.Component, BaseUnitTarget.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        const int frequency = 10;

        BaseUnitSearchSystem searchSystem = null;
        BaseUnitSearchSystem SearchSystem
        {
            get
            {
                searchSystem = searchSystem ?? this.World.GetExistingSystem<BaseUnitSearchSystem>();

                return searchSystem;
            }
        }

        Dictionary<long, List<FixedPointVector3>> EnemyPositionsContainer
        {
            get { return this.SearchSystem?.EnemyPositionsContainer; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                ComponentType.ReadOnly<UnitActionData>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<GunComponent.HasAuthority>(),
                ComponentType.ReadWrite<PostureAnimation.Component>(),
                ComponentType.ReadOnly<PostureAnimation.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<PostureBoneContainer>(),
                ComponentType.ReadOnly<SpatialEntityId>()), frequency);

            action = Query;

            
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query (Entity entity,
                            ref UnitActionData action,
                            ref GunComponent.Component gun,
                            ref PostureAnimation.Component anim,
                            ref BaseUnitTarget.Component target,
                            ref BaseUnitStatus.Component status,
                            ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Side == UnitSide.None)
                return;

            if (UnitUtils.IsOffensive(status.Type) == false)
                return;

            if (target.State != TargetState.ActionTarget)
                return;

            var current = Time.ElapsedTime;

            var id = entityId.EntityId.Id;
            if (this.EnemyPositionsContainer == null ||
                this.EnemyPositionsContainer.TryGetValue(id, out var enemyPositions) == false)
                return;

            Vector3? epos = null;
            if (enemyPositions.Count > 0) {
                epos = enemyPositions[0].ToWorkerPosition(this.Origin);

                var container = EntityManager.GetComponentObject<PostureBoneContainer>(entity);
                Attack(container, current, epos.Value, entityId, ref gun, out var aimPos);
                epos = aimPos;
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

        void Attack(PostureBoneContainer container, double current, in Vector3 epos, in SpatialEntityId entityId, ref GunComponent.Component gun, out Vector3 aimPos)
        {
            aimPos = epos;
            var gunsDic = gun.GunsDic;
            var updGuns = false;

            if (container == null || container.Bones == null)
                return;

            foreach (var bone in container.Bones)
            {
                GunInfo gunInfo;
                if (gunsDic.TryGetValue(bone.hash, out gunInfo) == false)
                    continue;

                var result = CheckRange(container.GetCannon(bone.hash), epos, gunInfo.AttackRange(), gunInfo.AttackAngle(), gunInfo.BulletSpeed(), out aimPos);
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

        Result CheckRange(CannonTransform cannon, in Vector3 epos, float range, float angle, float velocity, out Vector3 aimPos)
        {
            aimPos = epos;
            var trans = cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            var foward = diff.normalized;
            if (Vector3.Angle(cannon.Forward, foward) > angle)
                return Result.Rotate;

            aimPos += Vector3.up * PhysicsUtils.CalcAimHeight(velocity, diff.magnitude);
            return Result.InRange;
        }
    }

    public struct UnitActionData : IComponentData
    {
        public float SightRange;
        public float AttackRange;
        public Vector3 TargetPosition;

        public static UnitActionData CreateData(float sightRange, float attackRange)
        {
            var data = new UnitActionData();
            data.SightRange = sightRange;
            data.AttackRange = attackRange;
            data.TargetPosition = Vector3.zero;
            return data;
        }
    }


}
