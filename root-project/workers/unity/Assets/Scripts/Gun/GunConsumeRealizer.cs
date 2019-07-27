using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk;

namespace AdvancedGears
{
    public class GunConsumeTrigger : MonoBehaviour
    {
        [Require] GunComponentWriter gunWriter;

        void OnEnable()
        {
            gunWriter.OnFireTriggeredEvent += OnTarget;
            gunWriter.OnBulletSuppliedEvent += OnSupply;
        }

        private void OnTarget(AttackTargetInfo info)
        {
            CommonUpdate(info.Attached, -1);
        }
        
        private void OnSupply(SupplyBulletInfo info)
        {
            CommonUpdate(info.Attached, info.Amount);
        }

        void CommonUpdate(PosturePoint attached, int num)
        {
            var dic = gunWriter.Data.GunsDic;
            GunInfo gun;
            if (dic.TryGetValue(attached, out gun) == false)
                return;

            gun.StockBullets = Mathf.Clamp(gun.StockBullets + num, 0, gun.StockMax);

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }
    }
}
