using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectRepresentation;
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
            // BaseUnit情報
            public ComponentDataArray<BaseUnit.Component> BaseUnit;
            // 権限情報
            [ReadOnly] public ComponentDataArray<Authoritative<BaseUnit.Component>> DenoteAuthority;
        }

        [Inject] private Data data;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }

        readonly float rotSpeed = 2.0f;
        readonly float moveSpeed = 1.0f;

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var rigidbody = data.RigidBody[i];
                var unitComponent = data.BaseUnit[i];

                var pos = rigidbody.position;

                var vec = unitComponent.MoveVelocity;
                var uVec = new Vector3(vec.X, vec.Y, vec.Z);

                var enemy = getNearestEnemeyPosition(unitComponent.Side, pos, 10);
                if (enemy != null)
                {
                    var diff =  enemy.Value - pos;
                    rotate(rigidbody.transform, diff, rotSpeed);
                    uVec = get_move_velocity(diff, moveSpeed * 3, moveSpeed) * rigidbody.transform.forward;
                }
                else
                {
                    uVec = Vector3.zero;
                }

                unitComponent.MoveVelocity = new Vector3f(uVec.x, uVec.y, uVec.z);
                data.BaseUnit[i] = unitComponent;

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

        Vector3? getNearestEnemeyPosition(uint self_side, Vector3 pos, float length)
        {
            float len = float.MaxValue;
            Vector3? e_pos = null;

            var colls = Physics.OverlapSphere(pos,length, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<SpatialOSComponent>();
                if (comp == null)
                    continue;

                if (EntityManager.HasComponent<BaseUnit.Component>(comp.Entity))
                {
                    var unit = EntityManager.GetComponentData<BaseUnit.Component>(comp.Entity);
                    if (unit.Side == self_side)
                        continue;

                    var t_pos = col.transform.position;
                    var l = (t_pos - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        e_pos = t_pos;
                    }
                }
            }

            return e_pos;
        }
    }
}
