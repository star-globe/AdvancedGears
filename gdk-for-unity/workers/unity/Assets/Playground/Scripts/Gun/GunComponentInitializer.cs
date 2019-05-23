using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class GunComponentInitializer : MonoBehaviour
    {
        [Require] GunComponentWriter gunWriter;

        [SerializeField]
        int[] gunIds;

        void Start()
        {
            Assert.IsNotNull(gunIds);

            var gunsList = gunIds.Select(id => GunDictionary.Instance.GetGunSettings(id)).ToArrya();
            Dictionary<PosturePoint,GunInfo> dic =  new Dictionary<PosturePoint,GunInfo>();
            long uid = 0;
            foreach (var gun in gunsList)
            {
                if (gun == null || gun.ContainsKey(gun.Attached))
                    continue;

                gun.Add(gun.Attached, gun.GetGunInfo(uid));
                uid++;
            }

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunDic = dic
            });
        }
    }
}
