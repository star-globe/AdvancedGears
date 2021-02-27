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
            CommonUpdate(info.AttachedBone, -1);
        }
        
        private void OnSupply(SupplyBulletInfo info)
        {
            CommonUpdate(info.AttachedBone, info.Amount);
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

        void CommonUpdate(int bone, int num)
        {
            var dic = gunWriter.Data.GunsDic;
            GunInfo gun;
            if (dic.TryGetValue(bone, out gun) == false)
                return;

            gun.AddBullets(num);
            dic[bone] = gun;

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }
    }
}
