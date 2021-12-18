using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;

namespace AdvancedGears
{
    public class LongRangeBulletReceiverRealizer : BulletFireBase
    {
        [Require] StrategyLongBulletReceiverReader reader;
        [Require] private World world;
        protected override World World => world;

        bool isInitialzed = false;
        void OnEnable()
        {
            reader.OnAddBulletEvent += Fire;
            reader.OnVanishBulletEvent += Vanish;
        }

        private void Initialize()
        {
            if (isInitialzed)
                return;

            foreach (var bullet in reader.Data.CurrentBullets) {
                Fire(bullet);
            }

            isInitialzed = true;
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
}
