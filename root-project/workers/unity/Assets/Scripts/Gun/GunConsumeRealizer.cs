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
            gunWriter.OnBulletDiffedEvent += OnDiffed;
        }

        private void OnTarget(AttackTargetInfo info)
        {
            CommonUpdate(info.Attached, -1);
        }
        
        private void OnSupply(SupplyBulletInfo info)
        {
            CommonUpdate(info.Attached, info.Amount);
        }

        private void OnDiffed(BulletDiffList list)
        {
            var dic = gunWriter.Data.GunsDic;
            foreach(var key in dic.Keys) {
                var index = list.Diffs.FindIndex(diff => diff.GunId == dic[key].GunId);
                if (index < 0)
                    continue;

                var num = list.Diffs[index].Diff;
                var gun = dic[key];

                gun.AddBullets(num);
                dic[key] = gun;
            }

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }

        void CommonUpdate(PosturePoint attached, int num)
        {
            var dic = gunWriter.Data.GunsDic;
            GunInfo gun;
            if (dic.TryGetValue(attached, out gun) == false)
                return;

            gun.AddBullets(num);
            dic[attached] = gun;

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }
    }
}
