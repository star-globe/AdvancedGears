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
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BulletMovementSystem : ComponentSystem
    {
        EntityQuery group;

        private WorkerSystem worker;

        public BulletCreator BulletCreator { get; private set; }

        protected override void OnCreateManager()
        {
            worker = World.GetExistingSystem<WorkerSystem>();

            var go = new GameObject("BulletCreator");
            BulletCreator = go.AddComponent<BulletCreator>();
            BulletCreator.Setup(this.EntityManager, worker.Origin);

            group = GetEntityQuery(
                ComponentType.ReadWrite<Rigidbody>(),
                ComponentType.ReadWrite<BulletInfo>()
           );
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Rigidbody rigid, ref BulletInfo info) =>
            {
                if (!info.IsActive)
                    return;

                // time check
                var diff = Time.time - info.LaunchTime;
                if (diff >= info.LifeTime)
                {
                    info.IsActive = false;
                    rigid.gameObject.SetActive(false);
                    return;
                }
            });
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
