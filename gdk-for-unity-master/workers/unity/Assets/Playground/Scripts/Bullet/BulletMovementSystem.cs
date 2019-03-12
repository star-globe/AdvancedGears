using Improbable;
using Improbable.Gdk.GameObjectRepresentation;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    internal class BulletMovementSystem : ComponentSystem
    {
        private struct Data
        {
            public readonly int Length;
            public ComponentArray<Collider> Collider;
            public ComponentDataArray<BulletInfo> BulletInfo;
        }

        [Inject] private Data data;

        BulletCreator creator;

        protected override void OnCreateManager()
        {
            var go = new GameObject("BulletCreator");
            creator = go.AddComponent<BulletCreator>();
            creator.Setup(this.EntityManager);
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var collider = data.Collider[i];
                var info = data.BulletInfo[i];

                if (!info.IsActive)
                    continue;

                // time check
                var diff = Time.realtimeSinceStartup - info.LaunchTime;
                if (diff >= info.LifeTime)
                {
                    info.IsActive = false;
                    data.BulletInfo[i] = info;
                    continue;
                }

                var trans = collider.transform;

                var vec = info.CurrentVelocity;
                var uVec = new Vector3(vec.X, vec.Y, vec.Z);

                trans.Translate(uVec * Time.deltaTime);

                // gravity
                uVec += Physics.gravity * Time.deltaTime;
                info.CurrentVelocity = new Vector3f(uVec.x, uVec.y, uVecz);

                data.BulletInfo[i] = info;
                //var enemy = getNearestEnemeyPosition(unitComponent.Side, pos, 10);
                //if (enemy != null)
                //{
                //    var diff = enemy.Value - pos;
                //    rotate(rigidbody.transform, diff, rotSpeed);
                //    uVec = get_move_velocity(diff, moveSpeed * 3, moveSpeed) * rigidbody.transform.forward;
                //}
                //else
                //{
                //    uVec = Vector3.zero;
                //}
                //
                //unitComponent.MoveVelocity = new Vector3f(uVec.x, uVec.y, uVec.z);
                //data.BaseUnit[i] = unitComponent;
                //
                //rigidbody.MovePosition(pos + uVec * Time.fixedDeltaTime);
            }
        }
    }

    public struct BulletInfo : IComponentData
    {
        public int Power;
        public uint Type;
        public uint Alignment;
        public Improbable.Vector3f LaunchPosition;
        public Improbable.Vector3f InitialVelocity;
        public Improbable.Vector3f CurrentVelocity;
        public float LaunchTime;
        public float LifeTime;
        public uint GunId;
        public long ShooterEntityId;
        public bool IsActive;

        public BulletInfo(BulletFireInfo fire)
        {
            Power = fire.Power;
            Type = fire.Type;
            Alignment = fire.Alignment;
            LaunchPosition = fire.LaunchPosition;
            InitialVelocity = fire.InitialVelocity;
            CurrentVelocity = InitialVelocity;
            LaunchTime = fire.LaunchTime;
            LifeTime = fire.LifeTime;
            GunId = fire.GunId;
            ShooterEntityId = fire.ShooterEntityId;
            IsActive = true;
        }
    }
}
