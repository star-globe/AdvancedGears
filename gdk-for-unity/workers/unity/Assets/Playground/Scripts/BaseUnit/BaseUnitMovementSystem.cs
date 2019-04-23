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
            public ComponentArray<Transform> Transform;
            [ReadOnly] public ComponentDataArray<BaseUnitMovement.Component> Movement;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            [ReadOnly] public ComponentDataArray<BaseUnitAction.Component> Action;
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
                var trans = data.Transform[i];
                var unit = trans.GetComponent<UnitTransform>();
                if (unit == null)
                    continue;

                var rigidbody = unit.Vehicle;
                var movement = data.Movement[i];
                var status = data.Status[i];
                var action = data.Action[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Soldier &&
                    status.Type != UnitType.Commander)
                    continue;

                if (!movement.IsTarget)
                {
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.angularVelocity = Vector3.zero;
                    continue;
                }

                var pos = rigidbody.position;
                var tgt = movement.TargetPosition.ToUnityVector() - origin;

                // modify target
                if (movement.TargetInfo.CommanderId.IsValid())
                {
                    var com = movement.CommanderPosition.ToUnityVector() - origin;
                    tgt = get_nearly_position(pos, tgt, com, movement.TargetInfo.AllyRange);
                }

                int foward = 0;
                var diff = tgt - pos;
                var range = action.AttackRange;
                switch (status.Order) {
                    case OrderType.Move:    range = 0.0f;   break;
                    case OrderType.Attack:  range *= 0.8f;  break;
                    case OrderType.Escape:  range *= 1.5f;  break;
                    case OrderType.Keep:    range *= 1.1f;  break;
                }

                var min_range = range * 0.9f;
                var mag = diff.sqrMagnitude;

                if (mag > range * range)
                    foward = 1;
                else if (mag < min_range * min_range)
                    foward = -1;

                rotate(rigidbody.transform, tgt - pos, movement.RotSpeed);

                var uVec = rigidbody.transform.forward * movement.MoveSpeed * foward;

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
