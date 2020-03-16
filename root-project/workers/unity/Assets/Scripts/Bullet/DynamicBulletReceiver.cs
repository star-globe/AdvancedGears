using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    /// <summary>
    /// Reciever for dynamic object
    /// </summary>
    public class DynamicBulletReceiver : BulletHitReceiver
    {
        [Require] BaseUnitHealthCommandSender healthCommandSender;
        [Require] BaseUnitHealthReader healthReader;
        [Require] private EntityId entityId;
        [Require] World world;
        protected override World World => world;

        [SerializeField]
        HitNotifiersInfo notifiers;

        protected override void OnHit(BulletInfo info)
        {
            var current = healthReader.Data.Health;
            current -= info.Power;
            if (current < 0)
                current = 0;

            healthCommandSender.SendModifyHealthCommand(entityId, new HealthModifier(0, current));
        }

        protected override bool CheckSelf(long id)
        {
            return entityId.Id == id;
        }

        void Start()
        {
            foreach (var n in notifiers.Notifiers)
            {
                if (n != null)
                    n.OnCollisionEvent += OnCollisionEnter;
            }
        }

        void OnDestroy()
        {
            foreach (var n in notifiers.Notifiers)
            {
                if (n != null)
                    n.OnCollisionEvent -= OnCollisionEnter;
            }
        }
    }

    public abstract class BulletHitReceiver : BulletFireBase
    {
        const string bulletTag = "Bullet";

        protected void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag != bulletTag)
                return;

            var fire = other.gameObject.GetComponent<BulletFireComponent>();
            if (fire == null)
                return;

            if (CheckSelf(fire.Value.ShooterEntityId))
                return;

            OnHit(fire.Value);
            base.Creator?.InvokeVanishAction(fire.Value.ShooterEntityId, fire.Value.Type, fire.Value.BulletId);
        }

        protected virtual void OnHit(BulletInfo info)
        {
        }

        protected virtual bool CheckSelf(long id)
        {
            return false;
        }
    }
}
