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
    internal class BaseUnitMovementSystem : SpatialComponentSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<BaseUnitPosture.Component>(),
                    ComponentType.ReadOnly<BaseUnitPosture.ComponentAuthority>(),
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<BaseUnitAction.Component>(),
                    ComponentType.ReadWrite<FuelComponent.Component>(),
                    ComponentType.ReadOnly<FuelComponent.ComponentAuthority>()
            );

            group.SetFilter(BaseUnitPosture.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitPosture.Component posture,
                                          ref BaseUnitMovement.Component movement,
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

                var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);

                if (target.State == TargetState.None)
                {
                    rigidbody.Stop();
                    return;
                }

                //var pos = rigidbody.position;
                //
                //Vector3 tgt;
                //if (target.State == TargetState.OutOfRange && movement.BoidVector.Vector != FixedPointVector3.Zero) {
                //    var boidVec = movement.BoidVector.GetVector3(movement.TargetRange);
                //    tgt = pos + boidVec;
                //}
                //else
                //    tgt = movement.TargetPosition.ToWorkerPosition(this.Origin);
                //
                //float forward = 0.0f;
                //var diff = tgt - pos;
                //var range = movement.TargetRange;
                //var buffer = range * RangeDictionary.MoveBufferRate;
                //var mag = diff.magnitude;
                //
                //if (mag > range) {
                //    forward = Mathf.Min((mag - range) / buffer, 1.0f);
                //}
                //else if (mag < range - buffer) {
                //    forward = Mathf.Max((mag - range + buffer) / buffer , -1.0f);
                //}
                //
                //bool isRotating = false;
                //if (rotate(rigidbody.transform, tgt - pos, movement.RotSpeed))
                //{
                //    var inter = posture.Interval;
                //    if (posture.Initialized && inter.CheckTime())
                //    {
                //        posture.Interval = inter;
                //        posture.Root = rigidbody.transform.rotation.ToCompressedQuaternion();
                //    }
                //    isRotating = true;
                //}
                //
                //if (forward == 0.0f)
                //{
                //    rigidbody.Stop(isRotating);
                //    return;
                //}
                //
                //var speed = AttackLogicDictionary.GetOrderSpeed(status.Order, movement.MoveSpeed);
                //
                //var uVec = rigidbody.transform.forward * speed * forward;
                //var moveVec = uVec * Time.fixedDeltaTime;
                //rigidbody.MovePosition(pos + moveVec);


                var trans = rigidbody.transform;
                var uVec = trans.forward * movement.MoveSpeed;

                rigidbody.velocity = uVec;

                if (movement.RotSpeed != 0.0f)
                    trans.Rotate(trans.up, movement.RotSpeed * Time.fixedDeltaTime);

                var moveVec = uVec * Time.fixedDeltaTime;

                var consume = (int)(moveVec.magnitude * movement.ConsumeRate);
                fuel.Fuel -= consume;
                if (fuel.Fuel < 0)
                    fuel.Fuel = 0;
            });
        }

        bool in_range(Vector3 forward, Vector3 tgt, float range, out Vector3 rot)
        {
            rot = Vector3.Cross(forward, tgt);

            if (Vector3.Dot(forward, tgt) < 0.0f)
                return false;

            return Mathf.Asin(rot.magnitude) < Mathf.Deg2Rad * range;
        }

        bool rotate(Transform transform, Vector3 diff, float rot_speed)
        {
            Vector3 rot;
            Vector3 foward = diff.normalized;
            float angle = rot_speed * Time.deltaTime;
            if (in_range(transform.forward, foward, angle, out rot) == false)
            {
                RotateLogic.Rotate(transform, foward, angle);
                return true;
            }

            return false;
        }

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
