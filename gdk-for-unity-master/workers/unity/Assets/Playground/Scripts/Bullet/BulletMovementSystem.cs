using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    internal class BulletMovementSystem : ComponentSystem
    {
        private struct Data
        {
            public readonly int Length;
            public ComponentArray<Rigidbody> RigidBody;
            public ComponentDataArray<BulletComponent.Component> BulletInfo;
        }

        [Inject] private Data data;

        protected override void OnUpdate()
        {
        }
    }
}
