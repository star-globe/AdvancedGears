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
    public class LongRangeBulletReceiverContainer : MonoBehaviour
    {
        [Require] StrategyLongBulletReceiverWriter writer;

        bool isInitialzed = false;
        void OnEnable()
        {
            writer.OnAddBulletEvent += Add;
            writer.OnVanishBulletEvent += Vanish;
        }

        private void Add(BulletFireInfo info)
        {
            var bullets = writer.Data.CurrentBullets;
            foreach (var b in bullets) {
                // check
                if (b.ShooterEntityId.Equals(info.ShooterEntityId) &&
                    b.BulletId.Equals(info.BulletId))
                    return;
            }

            bullets.Add(info);

            writer.SendUpdate(new StrategyLongBulletReceiver.Update()
            {
                CurrentBullets = bullets,
            });
        }

        private void Vanish(BulletVanishInfo info)
        {
            var bullets = writer.Data.CurrentBullets;
            int index = -1;
            for (int i = 0; i < bullets.Count; i++) {
                var b = bullets[i];

                // check
                if (b.ShooterEntityId.Equals(info.ShooterEntityId) &&
                    b.BulletId.Equals(info.BulletId)) {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return;

            bullets.RemoveAt(index);

            writer.SendUpdate(new StrategyLongBulletReceiver.Update()
            {
                CurrentBullets = bullets,
            });
        }
    }
}
