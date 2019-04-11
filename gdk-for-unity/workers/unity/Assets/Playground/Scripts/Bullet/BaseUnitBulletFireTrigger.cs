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
        [Require] BaseUnitActionReader actionReader;
        [Require] BulletComponentWriter bulletWriter;
        [Require] World world;

        protected override BulletComponentWriter BulletWriter => bulletWriter;
        protected override World World => world;

        private Vector3 target = Vector3.zero;

        protected override void OnEnable()
        {
            base.OnEnable();
            actionReader.OnFireTriggeredEvent += OnTarget;
        }

        private void OnTarget(AttackTargetInfo info)
        {
            // rotate to target
            if (target.x != info.TargetPosition.X ||
                target.y != info.TargetPosition.Y ||
                target.z != info.TargetPosition.Z)
            {
                target.Set(info.TargetPosition.X,
                            info.TargetPosition.Y,
                            info.TargetPosition.Z);
            }

            var diff = target + origin - this.MuzzleTransform.position;
            this.MuzzleTransform.forward = diff.normalized;
            OnFire();
        }
    }
}
