using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitSightSystem : SpatialComponentSystem
    {
        EntityQuery group;
        IntervalChecker interval;
        float deltaTime = -1.0f;

        const int period = 10; 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitMovement.ComponentAuthority>(),
                    ComponentType.ReadOnly<BaseUnitSight.Component>(),
                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<BaseUnitAction.Component>(),
                    ComponentType.ReadOnly<FuelComponent.Component>()
            );

            group.SetFilter(BaseUnitMovement.ComponentAuthority.Authoritative);

            interval = IntervalCheckerInitializer.InitializedChecker(period);

            deltaTime = Time.time;
        }

        protected override void OnUpdate()
        {
            if (interval.CheckTime() == false)
                return;

            deltaTime = Time.time - deltaTime;

            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status,
                                          ref FuelComponent.Component fuel) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Soldier &&
                    status.Type != UnitType.Commander)
                    return;

                // check fueld
                if (fuel.Fuel == 0)
                    return;

                var unit = EntityManager.GetComponentObject<UnitTransform>(entity);

                // check ground
                if (unit == null || unit.GetGrounded(out var hitInfo) == false)
                    return;

                if (target.State == TargetState.None)
                    return;

                var trans = unit.transform;
                var pos = trans.position;

                Vector3 tgt;
                if (target.State == TargetState.OutOfRange && sight.BoidVector.Vector != FixedPointVector3.Zero) {
                    var boidVec = sight.BoidVector.GetVector3(sight.TargetRange);
                    tgt = pos + boidVec;
                }
                else
                    tgt = sight.TargetPosition.ToWorkerPosition(this.Origin);

                float forward = 0.0f;
                var diff = tgt - pos;
                var range = sight.TargetRange;
                var buffer = range * RangeDictionary.MoveBufferRate;
                var mag = diff.magnitude;

                if (mag > range) {
                    forward = Mathf.Min((mag - range) / buffer, 1.0f);
                }
                else if (mag < range - buffer) {
                    forward = Mathf.Max((mag - range + buffer) / buffer , -1.0f);
                }

                MovementDictionary.TryGet(status.Type, out var speed, out var rot);

                var isRotate = rotate(trans, tgt - pos, rot * Time.deltaTime);

                if (forward == 0.0f)
                    movement.MoveSpeed = 0.0f;
                else
                    movement.MoveSpeed = forward * speed;

                if (isRotate == 0)
                    movement.RotSpeed = 0.0f;
                else
                    movement.RotSpeed = rot * isRotate;
            });

            deltaTime = Time.time;
        }

        bool in_range(Vector3 forward, Vector3 tgt, float range, out Vector3 rot)
        {
            rot = Vector3.Cross(forward, tgt);

            if (Vector3.Dot(forward, tgt) < 0.0f)
                return false;

            return Mathf.Asin(rot.magnitude) < Mathf.Deg2Rad * range;
        }

        int rotate(Transform transform, Vector3 diff, float angle_range)
        {
            var rot = RotateLogic.GetAngle(transform.up, transform.forward, diff.normalized);
            if (rot * rot < angle_range * angle_range)
                return 0;

            return rot < 0 ? -1: 1;
        }
        //bool rotate(Transform transform, Vector3 diff, float rot_speed)
        //{
        //    Vector3 rot;
        //    Vector3 foward = diff.normalized;
        //    float angle = rot_speed * Time.deltaTime;
        //    if (in_range(transform.forward, foward, angle, out rot) == false)
        //    {
        //        RotateLogic.Rotate(transform, foward, angle);
        //        return true;
        //    }
        //
        //    return false;
        //}

        float get_move_velocity(Vector3 diff, float check_length, float speed)
        {
            int v = 0;
            var len = diff.magnitude;
            if (len >= check_length)
                v = 1;
            else if (len < check_length * 0.75f)
                v = -1;

            var sp = speed;
            if (v != 0 && diff.magnitude > speed * 0.5f)
            {
                if (v < 0)
                    speed *= 0.7f;
            }

            return v * speed;
        }

        Vector3 get_nearly_position(Vector3 pos, Vector3 tgt, Vector3 com, float range)
        {
            var diff = pos - com;
            var length = diff.magnitude;
            if (length < range)
                return tgt;

            var rev = -1 * diff.normalized * (length - range);

            return tgt + rev;
        }
    }
}
