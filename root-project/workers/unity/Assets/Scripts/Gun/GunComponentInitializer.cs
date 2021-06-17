using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public struct GunIdPair
    {
        public uint Id;
        public int bone;
    }


    public class GunComponentInitializer : MonoBehaviour
    {
        [Require] GunComponentWriter gunWriter;

        public void SetGunIds(GunIdPair[] gunIds)
        {
            if (gunIds == null)
                return;

            var gunsList = gunIds.Select(pair => GunDictionary.GetGunSettings(pair.Id)).ToArray();
            Dictionary<int,GunInfo> dic =  new Dictionary<int,GunInfo>();
            ulong uid = 0;
            foreach (var pair in gunIds)
            {
                var gun = GunDictionary.GetGunSettings(pair.Id);
                if (gun == null || dic.ContainsKey(pair.bone))
                    continue;

                dic.Add(pair.bone, gun.GetGunInfo(uid, pair.bone));
                uid++;
            }

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }

        public float AttackRange
        {
            get
            {
                if (gunWriter == null)
                    return 0;

                return gunWriter.Data.GetAttackRange();
            }
        }
    }
}
