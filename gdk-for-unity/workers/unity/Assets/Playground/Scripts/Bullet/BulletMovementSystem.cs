using Improbable;
using Improbable.Gdk.Core;
using System;
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
        ComponentGroup group;

        private WorkerSystem worker;

        public BulletCreator BulletCreator { get; private set; }

        protected override void OnCreateManager()
        {
            worker = World.GetExistingManager<WorkerSystem>();

            var go = new GameObject("BulletCreator");
            BulletCreator = go.AddComponent<BulletCreator>();
            BulletCreator.Setup(this.EntityManager, worker.Origin);

            group = GetComponentGroup(
                ComponentType.Create<Rigidbody>(),
                ComponentType.Create<BulletInfo>()
           );
        }

        protected override void OnUpdate()
        {
            var rigidData = group.GetComponentArray<Rigidbody>();
            var bulletData = group.GetComponentDataArray<BulletInfo>();

            for (var i = 0; i < rigidData.Length; i++)
            {
                var rigid = rigidData[i];
                var info = bulletData[i];

                if (!info.IsActive)
                    continue;

                // time check
                var diff = Time.realtimeSinceStartup - info.LaunchTime;
                if (diff >= info.LifeTime)
                {
                    info.IsActive = false;
                    rigid.gameObject.SetActive(false);
                    bulletData[i] = info;
                    continue;
                }

                //var vec = info.CurrentVelocity;
                //var uVec = new Vector3(vec.X, vec.Y, vec.Z);

                //var pos = rigid.position;
                //rigid.MovePosition(pos + uVec * Time.fixedDeltaTime);

                //// gravity
                //uVec += Physics.gravity * Time.deltaTime;
                //info.CurrentVelocity = new Vector3f(uVec.x, uVec.y, uVec.z);

                //data.BulletInfo[i] = info;
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

    [Serializable]
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
        public ulong BulletId;
        public byte active;

        public bool IsActive
        {
            get { return active == 1; }
            set
            {
                if (value)
                    active = 1;
                else
                    active = 0;
            }
        }

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
            BulletId = fire.BulletId;
            active = 1;
        }

        public BulletInfo(BulletInfo info, byte act)
        {
            Power = info.Power;
            Type = info.Type;
            Alignment = info.Alignment;
            LaunchPosition = info.LaunchPosition;
            InitialVelocity = info.InitialVelocity;
            CurrentVelocity = info.CurrentVelocity;
            LaunchTime = info.LaunchTime;
            LifeTime = info.LifeTime;
            GunId = info.GunId;
            ShooterEntityId = info.ShooterEntityId;
            BulletId = info.BulletId;
            active = act;
        }
    }
}
