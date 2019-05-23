using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable;
using Improbable.Gdk.Core;

namespace Playground
{
    public class BaseUnitBulletFireTrigger : BulletFireTriggerBase
    {
        [Require] GunComponentReader gunReader;
        [Require] BulletComponentWriter bulletWriter;
        [Require] World world;

        protected override BulletComponentWriter BulletWriter => bulletWriter;
        protected override World World => world;

        private Vector3 target = Vector3.zero;

        protected override void OnEnable()
        {
            base.OnEnable();
            gunReader.OnFireTriggeredEvent += OnTarget;
        }

        private void OnTarget(AttackTargetInfo info)
        {
            OnFire(info.Attached);
        }
    }
}
