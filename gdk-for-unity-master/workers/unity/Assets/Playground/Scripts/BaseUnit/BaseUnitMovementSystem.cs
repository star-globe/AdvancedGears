using Improbable;
using Improbable.Gdk.Core;
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
            public ComponentDataArray<BaseUnitMoveVelocity.Component> BaseUnit;
            // 権限情報
            [ReadOnly] public ComponentDataArray<Authoritative<BaseUnitMoveVelocity.Component>> DenoteAuthority;
        }

        [Inject] private Data data;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }

        readonly float rotSpeed = 20.0f;

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var rigidbody = data.RigidBody[i];
                var unitComponent = data.BaseUnit[i];

                var vec = unitComponent.MoveVelocity;
                var uVec = new Vector3(vec.X, vec.Y, vec.Z);
                uVec = Quaternion.Euler(0, rotSpeed * Time.fixedDeltaTime, 0) * uVec;

                unitComponent.MoveVelocity = new Vector3f(uVec.x, uVec.y, uVec.z);
                data.BaseUnit[i] = unitComponent;

                var pos = rigidbody.position;
                rigidbody.MovePosition(pos + uVec * Time.fixedDeltaTime);
            }
        }
    }
}
