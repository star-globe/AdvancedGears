using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk;

namespace AdvancedGears
{
    public class BulletFireRealizer : BulletFireBase
    {
        [Require] BulletComponentReader reader;
        [Require] private World world;
        protected override World World => world;

        void OnEnable()
        {
            reader.OnFiresEvent += Fire;
            reader.OnVanishesEvent += Vanish;
        }

        private void Fire(BulletFireInfo info)
        {
            base.Creator?.OnFire(info);
        }

        private void Vanish(BulletVanishInfo info)
        {
            base.Creator?.OnVanish(info);
        }
    }

    public abstract class BulletFireBase : MonoBehaviour
    {
        protected abstract World World { get; }

        BulletMovementSystem bulletSystem = null;
        BulletMovementSystem BulletSystem
        {
            get
            {
                if (World == null)
                    return null;

                bulletSystem = bulletSystem ?? World.GetExistingSystem<BulletMovementSystem>();
                return bulletSystem;
            }
        }

        protected BulletCreator Creator
        {
            get
            {
                if (this.BulletSystem == null)
                    return null;

                return this.BulletSystem.BulletCreator;
            }
        }
    }
}
