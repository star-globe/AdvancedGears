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

        Ray vertical = new Ray();
        //readonly int layer = //LayerMask.//LayerMask.GetMask("Ground");

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitPosture.Component posture,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitAction.Component action,
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

                // set grounded normal
                var normal = hitInfo.normal;

                if (!movement.IsTarget)
                {
                    rigidbody.Stop();
                    rigidbody.transform.SetUpwards(normal);
                    return;
                }

                var pos = rigidbody.position;

                var tgt = movement.TargetPosition.ToWorkerPosition(this.Origin);

                // modify target
                if (action.IsTarget == false && target.TargetInfo.CommanderId.IsValid())
                {
                    var com = movement.CommanderPosition.ToWorkerPosition(this.Origin);
                    tgt = get_nearly_position(pos, tgt, com, target.TargetInfo.AllyRange);
                }

                int forward = 0;
                var diff = tgt - pos;
                var range = movement.TargetRange;
                var min_range = range * RangeDictionary.MoveRangeRate;
                var mag = diff.sqrMagnitude;

                if (mag > range * range)
                    forward = 1;
                else if (mag < min_range * min_range)
                    forward = -1;

                bool isRotating = false;
                if (rotate(rigidbody.transform, tgt - pos, movement.RotSpeed, normal))
                {
                    var inter = posture.Interval;
                    if (posture.Initialized && inter.CheckTime())
                    {
                        posture.Interval = inter;
                        posture.Root = rigidbody.transform.rotation.ToCompressedQuaternion();
                    }
                    isRotating = true;
                }

                if (forward == 0)
                {
                    rigidbody.Stop(isRotating);
                    return;
                }

                var uVec = rigidbody.transform.forward * movement.MoveSpeed * forward;
                var moveVec = uVec * Time.fixedDeltaTime;
                rigidbody.MovePosition(pos + moveVec);

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

        bool rotate(Transform transform, Vector3 diff, float rot_speed, Vector3 normal)
        {
            Vector3 rot;
            Vector3 foward = diff.normalized;
            float angle = rot_speed * Time.deltaTime;
            if (in_range(transform.forward, foward, angle, out rot) == false)
            {
                RotateLogic.Rotate(transform, foward, angle);
                transform.SetUpwards(normal);
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
