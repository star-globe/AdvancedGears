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
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    internal class BaseUnitMovementSystem : ComponentSystem
    {
        private struct Data
        {
            // データ長
            public readonly int Length;
            // 剛体配列
            public ComponentArray<Rigidbody> RigidBody;
            [ReadOnly] public ComponentDataArray<BaseUnitMovement.Component> Movement;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            // 権限情報
            [ReadOnly] public ComponentDataArray<Authoritative<BaseUnitMovement.Component>> DenoteAuthority;
        }

        [Inject] private Data data;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var rigidbody = data.RigidBody[i];
                var movement = data.Movement[i];
                var status = data.Status[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (!movement.IsTarget)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    continue;
                }

                var pos = rigidbody.position;

                var tgt = new Vector3( movement.TargetPosition.X,
                                       movement.TargetPosition.Y,
                                       movement.TargetPosition.Z);

                rotate(rigidbody.transform, tgt - pos, movement.RotSpeed);

                var uVec = rigidbody.transform.forward * movement.MoveSpeed;

                rigidbody.MovePosition(pos + uVec * Time.fixedDeltaTime);
            }
        }

        bool in_range(Vector3 forward, Vector3 tgt, float range, out Vector3 rot)
        {
            rot = Vector3.Cross(forward, tgt);

            if (Vector3.Dot(forward, tgt) < 0.0f)
                return false;

            return Mathf.Asin(rot.magnitude) < Mathf.Deg2Rad * range;
        }

        void rotate(Transform transform, Vector3 diff, float rot_speed)
        {
            Vector3 rot;
            if (in_range(transform.forward, diff.normalized, rot_speed, out rot) == false)
            {
                var v = Vector3.Dot(rot, Vector3.up) > 0 ? 1 : -1;
                transform.Rotate(Vector3.up, v * rot_speed);
            }
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
    }
}
