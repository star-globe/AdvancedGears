using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk;

namespace Playground
{
    public class GunConsumeTrigger : MonoBehaviour
    {
        [Require] GunComponentWriter gunWriter;
        [Require] private World world;

        void OnEnable()
        {
            gunWriter.OnFireTriggeredEvent += OnTarget;
        }

        private void OnTarget(AttackTargetInfo info)
        {
            var dic = gunWriter.Data.GunsDic;
            GunInfo gun;
            if (dic.TryGetValue(info.Attached, out gun) == false)
                return;

            gun.StockBullets--;

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }
    }
}
