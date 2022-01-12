using System;
using System.Collections;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class GigantUnitMoveSystem : BaseSearchSystem
    {
        EntityQuerySet group;
        EntityQueryBuilder.F_EDDDDD<MovementData, NavPathData, GigantComponent.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = new EntityQuerySet(GetEntityQuery(
                                             ComponentType.ReadOnly<GigantComponent.Component>(),
                                             ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                             ComponentType.ReadOnly<Transform>(),
                                             ComponentType.ReadOnly<SpatialEntityId>()
                                             ), 1.0f);
            action = Query;
        }

        protected override void OnUpdate()
        {
            HandleCaputure();
        }

        void HandleCaputure()
        {
            if (CheckTime(ref group.inter) == false)
                return;

            Entities.With(group.group).ForEach(action);
        }

        private void Query(Unity.Entities.Entity entity,
                                 ref MovementData movement,
                                 ref NavPathData path,
                                 ref GigantComponent.Component gigant,
                                 ref BaseUnitStatus.Component status,
                                 ref SpatialEntityId entityId)
        {
            if (status.Side != UnitSide.None)
                return;

            if (status.Type != UnitType.Gigant)
                return;

            var index = gigant.RootIndex;
            if (index < 0 || index >= gigant.Roots.Count)
                return;

            var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
            if (unit == null)
                return;

            var trans = unit.transform;
            var pos = trans.position;

            var tgt = gigant.Roots[index].ToWorkerPosition(this.Origin);

            tgt = navPathContainer.CheckNavPathAndTarget(tgt, pos, unit.SizeRadius, entityId.EntityId.Id, WalkableNavArea, ref path);

            var positionDiff = tgt - pos;

            var forward = MovementUtils.get_forward(positionDiff, 0, trans.forward);

            MovementDictionary.TryGet(status.Type, out var speed, out var rot);

            var isRotate = rotate(rot, trans, positionDiff);

            if (forward != 0.0f)
                movement.MoveSpeed = forward * speed;

            if (isRotate != 0)
                movement.RotSpeed = rot * isRotate;
        }

        /// <summary>
        /// get rotate info
        /// </summary>
        /// <param name="rotSpeed"></param>
        /// <param name="trans"></param>
        /// <param name="diff"></param>
        /// <returns></returns>
        int rotate(float rotSpeed, Transform trans, Vector3 diff)
        {
            var rate = MovementDictionary.RotateLimitRate;
            return MovementUtils.rotate(trans, diff, rotSpeed * Time.DeltaTime, rate, out var is_over);
        }

        NavPathContainer navPathContainer = new NavPathContainer();
    }
}
