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
            var dic = gunWriter.Data.GunsDIc;
            GunInfo info;
            if (dic.TryGetValue(info.Attached, oput info) == false)
                

            gunWriter.Update(new GunComponent.Update
            {
                
            });
        }
    }
}
