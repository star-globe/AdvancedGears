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
    public class LongRangeBulletFireRealizer : BulletFireBase
    {
        [Require] BulletComponentReader reader;
        [Require] LongRangeBulletComponentReader longRangeBulletReader;
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

            var settings = GunDictionary.GetGunSettings(info.GunId);
            if (settings != null && settings.IsLongRange) {
                var id = longRangeBulletReader.Data.ReceiverId;
                SendLongRangeBullet(id, info);
            }
        }

        private void Vanish(BulletVanishInfo info)
        {
            base.Creator?.OnVanish(info);
        }
    }
}
