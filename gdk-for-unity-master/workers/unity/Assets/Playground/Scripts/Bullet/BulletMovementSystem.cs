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
        }
    }

    public struct BulletInfo : IComponentData
    {
        int Power;
        uint Type;
        uint Alignment;
        Improbable.Vector3f LaunchPosition;
        Improbable.Vector3f InitialVelocity;
        Improbable.Vector3f CurrentVelocity;
        float LaunchTime;
        float LifeTime;
        uint GunId;
        long ShooterEntityId;

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
        }
    }
}
