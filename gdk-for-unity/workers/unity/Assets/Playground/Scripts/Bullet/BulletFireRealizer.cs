using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk;

namespace Playground
{
    public class BulletFireRealizer : BulletFireBase
    {
        [Require] BulletComponentReader reader;
        [Require] private World world;
        protected override World World => world;

        protected override void OnEnable()
        {
            base.OnEnable();
            reader.OnFiresEvent += Fire;
        }

        private void Fire(BulletFireInfo info)
        {
            base.Creator.OnFire(info);
        }
    }

    public abstract class BulletFireBase : MonoBehaviour
    {
        protected abstract World World { get; }

        BulletMovementSystem bulletSystem;
        protected BulletCreator Creator { get { return bulletSystem.BulletCreator; } }

        private void Start()
        {
            Assert.IsNotNull(bulletSystem);
        }

        protected virtual void OnEnable()
        {
            bulletSystem = World.GetExistingManager<BulletMovementSystem>();
        }

    }
}
