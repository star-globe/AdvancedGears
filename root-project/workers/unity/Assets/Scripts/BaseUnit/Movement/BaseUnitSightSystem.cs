using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitSightSystem : SpatialComponentSystem
    {
        EntityQuery group;
        IntervalChecker interval;
        double deltaTime = -1.0;

        const int period = 10; 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitMovement.HasAuthority>(),
                    ComponentType.ReadWrite<BaseUnitSight.Component>(),
                    ComponentType.ReadOnly<BaseUnitSight.HasAuthority>(),
                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<BaseUnitAction.Component>()
            );

            interval = IntervalCheckerInitializer.InitializedChecker(period);

            deltaTime = Time.ElapsedTime;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref interval) == false)
                return;

            deltaTime = Time.ElapsedTime - deltaTime;

            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsAutomaticallyMoving(status.Type) == false)
                    return;

                var unit = EntityManager.GetComponentObject<UnitTransform>(entity);

                // check ground
                if (unit == null || unit.GetGrounded(out var hitInfo) == false)
                    return;

                if (target.State == TargetState.None)
                    return;

                var trans = unit.transform;
                var pos = trans.position;

                Vector3? tgt = calc_update_boid(ref sight, target.State, pos);

                if (tgt == null)
                    tgt = sight.TargetPosition.ToWorkerPosition(this.Origin);

                var positionDiff = tgt.Value - pos;

                var forward = get_forward(positionDiff, sight.TargetRange);

                MovementDictionary.TryGet(status.Type, out var speed, out var rot);

                var isRotate = rotate(rot, trans, positionDiff);

                if (forward == 0.0f)
                    movement.MoveSpeed = 0.0f;
                else
                    movement.MoveSpeed = forward * speed;

                if (isRotate == 0)
                    movement.RotSpeed = 0.0f;
                else
                    movement.RotSpeed = rot * isRotate;
            });

            deltaTime = Time.ElapsedTime;
        }

        #region method
        /// <summary>
        /// check in range
        /// </summary>
        /// <param name="forward"></param>
        /// <param name="tgt"></param>
        /// <param name="range"></param>
        /// <param name="rot"></param>
        /// <returns></returns>
        bool in_range(Vector3 forward, Vector3 tgt, float range, out Vector3 rot)
        {
            rot = Vector3.Cross(forward, tgt);

            if (Vector3.Dot(forward, tgt) < 0.0f)
                return false;

            return Mathf.Asin(rot.magnitude) < Mathf.Deg2Rad * range;
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
            return rotate(trans, diff, rotSpeed * Time.DeltaTime, rate, out var is_over);
        }

        /// <summary>
        /// get rotate info
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="diff"></param>
        /// <param name="angle_range"></param>
        /// <param name="over_rate"></param>
        /// <param name="is_over"></param>
        /// <returns></returns>
        int rotate(Transform transform, Vector3 diff, float angle_range, float over_rate, out bool is_over)
        {
            is_over = false;
            var rot = RotateLogic.GetAngle(transform.up, transform.forward, diff.normalized);
            var sqrtRot = rot * rot;
            var sqrtRange = angle_range * angle_range;
            if (sqrtRot < sqrtRange)
            {
                return 0;
            }

            is_over = sqrtRot > sqrtRange * over_rate * over_rate;
            return rot < 0 ? -1 : 1;
        }

        /// <summary>
        /// get forward info
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        float get_forward(Vector3 diff, float range)
        {
            float forward = 0.0f;
            var buffer = range * RangeDictionary.MoveBufferRate;
            var mag = diff.magnitude;

            if (mag > range)
            {
                forward = Mathf.Min((mag - range) / buffer, 1.0f);
            }
            else if (mag < range - buffer)
            {
                forward = Mathf.Max((mag - range + buffer) / buffer, -1.0f);
            }

            return forward;
        }

        /// <summary>
        /// calculate target and update the boid info;
        /// </summary>
        /// <param name="sight"></param>
        /// <param name="targetState"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        Vector3? calc_update_boid(ref BaseUnitSight.Component sight, TargetState targetState, Vector3 pos)
        {
            Vector3? tgt = null;
            var boidVector = sight.BoidVector;

            if (boidVector.Potential > 0.0f)
            {
                var center = boidVector.Center.ToWorkerPosition(this.Origin);

                if ((center - pos).sqrMagnitude > boidVector.SqrtBoidRadius())
                    tgt = center;
                else if (targetState == TargetState.OutOfRange)
                    tgt = pos + boidVector.GetVector3(sight.TargetRange);
            }

            var current = Time.ElapsedTime;
            var diffTime = (float)(current - sight.BoidUpdateTime);
            boidVector.Potential = AttackLogicDictionary.ReduceBoidPotential(boidVector.Potential, diffTime);
            sight.BoidUpdateTime = current;
            sight.BoidVector = boidVector;

            return tgt;
        }
        #endregion
    }
}
