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

        protected override void OnEnable()
        {
            base.OnEnable();
            reader.OnFiresEvent += Fire;
        }

        private void Start()
        {
            Assert.IsNotNull(bulletSystem);
        }

        private void Fire(BulletFireInfo info)
        {
            base.Creator.OnFire(info);
        }
    }

    public abstract class BulletFireBase : MonoBehaviour
    {
        [Require] private World world; 

        BulletMovementSystem bulletSystem;
        protected BulletCreator Creator { get { return bulletSystem.BulletCreator; } }
        
        protected virtual void OnEnable()
        {
            bulletSystem = world.GetExistingManager<BulletMovementSystem>();
        }

    }
}
