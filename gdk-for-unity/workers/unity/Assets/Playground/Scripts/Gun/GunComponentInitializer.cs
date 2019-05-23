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
        uint[] gunIds;

        void Start()
        {
            Assert.IsNotNull(gunIds);

            var gunsList = gunIds.Select(id => GunDictionary.GetGunSettings(id)).ToArray();
            Dictionary<PosturePoint,GunInfo> dic =  new Dictionary<PosturePoint,GunInfo>();
            ulong uid = 0;
            foreach (var gun in gunsList)
            {
                if (gun == null || dic.ContainsKey(gun.Attached))
                    continue;

                dic.Add(gun.Attached, gun.GetGunInfo(uid));
                uid++;
            }

            gunWriter.SendUpdate(new GunComponent.Update
            {
                GunsDic = dic
            });
        }
    }
}
