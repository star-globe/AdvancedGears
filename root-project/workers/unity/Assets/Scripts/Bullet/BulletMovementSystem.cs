using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BulletMovementSystem : ComponentSystem
    {
        EntityQuery group;
        EntityQueryBuilder.F_CD<Rigidbody, BulletInfo> action;

        private WorkerSystem worker;

        public BulletCreator BulletCreator { get; private set; }

        protected override void OnCreate()
        {
            worker = World.GetExistingSystem<WorkerSystem>();

            var go = new GameObject("BulletCreator");
            BulletCreator = go.AddComponent<BulletCreator>();
            BulletCreator.Setup(this.EntityManager, worker.Origin);

            group = GetEntityQuery(
                ComponentType.ReadWrite<Rigidbody>(),
                ComponentType.ReadWrite<BulletInfo>()
           );

           action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach(action);
        }

        private void Query(Rigidbody rigid, ref BulletInfo info)
        {
            if (!info.IsActive)
                return;

            // time check
            var diff = Time.ElapsedTime - info.LaunchTime;
            if (diff >= info.LifeTime)
            {
                info.IsActive = false;
                rigid.gameObject.SetActive(false);
                return;
            }
        }
    }

    [Serializable]
    public struct BulletInfo : IComponentData
    {
        public int Power;
        public uint Type;
        public UnitSide Side;
        public FixedPointVector3 LaunchPosition;
        public FixedPointVector3 InitialVelocity;
        public FixedPointVector3 CurrentVelocity;
        public double LaunchTime;
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
            Side = fire.Side;
            LaunchPosition = fire.LaunchPosition;
            InitialVelocity = fire.InitialVelocity;
            CurrentVelocity = InitialVelocity;
            LaunchTime = fire.LaunchTime;
            LifeTime = fire.LifeTime();
            GunId = fire.GunId;
            ShooterEntityId = fire.ShooterEntityId;
            BulletId = fire.BulletId;
            active = 1;
        }

        public BulletInfo(BulletInfo info, byte act)
        {
            Power = info.Power;
            Type = info.Type;
            Side = info.Side;
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
