using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class SimpleGunComponentInitializer : MonoBehaviour
    {
        [Require] SimpleGunComponentWriter gunWriter;

        public void SetGunIds(GunIdPair[] gunIds)
        {
            if (gunIds == null)
                return;


        }
    }
}
