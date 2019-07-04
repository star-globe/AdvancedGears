using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitMovementSystem : ComponentSystem
    {
        EntityQuery group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                    ComponentType.ReadWrite<BaseUnitPosture.Component>(),
                    ComponentType.ReadOnly<BaseUnitPosture.ComponentAuthority>(),
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<BaseUnitAction.Component>()//,
                    //ComponentType.ReadWrite<FuelComponent.Component>(),
                    //ComponentType.ReadOnly<FuelComponent.ComponentAuthority>()
            );

            group.SetFilter(BaseUnitPosture.ComponentAuthority.Authoritative);
            //group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

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

                var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
                var rigidbody = unit.Vehicle;

                if (!movement.IsTarget)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    return;
                }

                // check fueld
                if (fuel.Fuel == 0)
                    return;

                var pos = rigidbody.position;

                // check ground
                var bounds = unit.GroundDetect.bounds;
                var up = unit.GroundDetect.transform.up;
                if (!Physics.Raycast(new Ray(bounds.center, -up), bounds.extents.y * 1.1f, LayerMask.GetMask("Ground")))
                    return;

                var tgt = movement.TargetPosition.ToUnityVector() - origin;

                // modify target
                if (action.IsTarget == false && target.TargetInfo.CommanderId.IsValid())
                {
                    var com = movement.CommanderPosition.ToUnityVector() - origin;
                    tgt = get_nearly_position(pos, tgt, com, target.TargetInfo.AllyRange);
                }

                int foward = 0;
                var diff = tgt - pos;
                var range = movement.TargetRange;
                var min_range = range * 0.9f;
                var mag = diff.sqrMagnitude;

                if (mag > range * range)
                    foward = 1;
                else if (mag < min_range * min_range)
                    foward = -1;

                if (rotate(rigidbody.transform, tgt - pos, movement.RotSpeed))
                {
                    var time = Time.realtimeSinceStartup;
                    var inter = posture.Interval;
                    if (posture.Initialized && inter.CheckTime(time))
                    {
                        posture.Interval = inter;
                        posture.Root = rigidbody.transform.rotation.ToImprobableQuaternion();
                    }
                }

                var uVec = rigidbody.transform.forward * movement.MoveSpeed * foward;
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
