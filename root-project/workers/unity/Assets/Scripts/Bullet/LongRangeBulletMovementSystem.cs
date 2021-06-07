using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LongRangeBulletMovementSystem : ComponentSystem
    {
        EntityQuery bulletQuery;
        private EntityQueryBuilder.F_ECD<Transform, LongRangeBulletComponent.Component> bulletAction;
        EntityQuery missileQuery;
        private EntityQueryBuilder.F_EDD<LongRangeBulletComponent.Component, GuidComponent.Component> missileAction;
        EntityQuery gravityQuery;
        private EntityQueryBuilder.F_ED<LongRangeBulletComponent.Component> gravityAction;

        float deltaTime = 0.0f;

        protected override void OnCreate()
        {
            bulletQuery = GetEntityQuery(
                ComponentType.ReadWrite<Transform>(),
                ComponentType.ReadOnly<LongRangeBulletComponent.Component>());

            missileQuery = GetEntityQuery(
                ComponentType.ReadWrite<LongRangeBulletComponent.Component>(),
                ComponentType.ReadOnly<GuidComponent.Component>());

            gravityQuery = GetEntityQuery(
                ComponentType.ReadWrite<LongRangeBulletComponent.Component>(),
                ComponentType.Exclude<GuidComponent.Component>());

            bulletAction = BulletQuery;
            missileAction = MissileQuery;
            gravityAction = GravityQuery;

            deltaTime = 0.0f;
        }

        protected override void OnUpdate()
        {
            deltaTime = Time.DeltaTime;
            UpdateBullet();
            UpdateMissile();
            UpdateGravity();
        }

        private void UpdateBullet()
        {
            Entities.With(bulletQuery).ForEach(bulletAction);
        }

        private void BulletQuery(Unity.Entities.Entity entity,
                                Transform trans,
                                ref LongRangeBulletComponent.Component bullet)
        {
            var speed = bullet.Speed;
            var vec = speed.ToUnityVector();
            trans.position += vec * deltaTime;
        }

        private void UpdateMissile()
        {
            Entities.With(missileQuery).ForEach(missileAction);
        }

        private void MissileQuery(Unity.Entities.Entity entity,
                                    ref LongRangeBulletComponent.Component bullet,
                                    ref GuidComponent.Component guid)
        {
            var speed = bullet.Speed;
        }

        private void UpdateGravity()
        {
            Entities.With(gravityQuery).ForEach(gravityAction);
        }

        private void GravityQuery(Unity.Entities.Entity entity,
                                    ref LongRangeBulletComponent.Component bullet)
        {
            if (bullet.IsFree == false)
                return;

            var speed = bullet.Speed;
            var vec = speed.ToUnityVector();
            vec -= Physics.gravity * deltaTime;
            bullet.Speed = vec.ToFixedPointVector3();
        }
    }
}
