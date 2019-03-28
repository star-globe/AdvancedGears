using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk;

namespace Playground
{
    public class BulletFireRealizer : MonoBehaviour
    {
        [Require] BulletComponentReader reader;
        [Require] private World world;

        BulletMovementSystem bulletSystem;
        BulletCreator Creator { get { return bulletSystem.BulletCreator; } }

        private void OnEnable()
        {
            reader.OnFiresEvent += Fire;
            bulletSystem = world.GetExistingManager<BulletMovementSystem>();
        }

        private void Start()
        {
            Assert.IsNotNull(bulletSystem);
        }

        private void Fire(BulletFireInfo info)
        {
            Creator.OnFire(info);
        }
    }
}
