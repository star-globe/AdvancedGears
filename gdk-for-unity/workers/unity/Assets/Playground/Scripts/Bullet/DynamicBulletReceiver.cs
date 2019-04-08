using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace Playground
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

        protected override void OnHit(BulletInfo info)
        {
            var current = healthReader.Data.Health;
            current -= info.Power;
            if (current < 0)
                current = 0;

            healthCommandSender.SendModifyHealthCommand(entityId, new HealthModifier(0, current));
        }
    }

    public abstract class BulletHitReceiver : BulletFireBase
    {
        const string bulletTag = "Bullet";

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag != bulletTag)
                return;

            var fire = other.gameObject.GetComponent<BulletFireComponent>();
            if (fire == null)
                return;

            OnHit(fire.Value);
            base.Creator.InvokeVanishAction(fire.Value.ShooterEntityId, fire.Value.BulletId);
            //var entity = objEntity.Entity;
            //var manager = objEntity.EntityManager;
            //var info = manager.GetComponentData<BulletInfo>(entity);

            // Damage

            // Destroy Bullet
            //manager.DestroyEntity(entity);

            // Destroy or Deactive pooled bullet object.
            // info.IsActive = false;
            // other.gameObject.SetActive(false);
            // manager.SetComponentData(entity, info);
        }

        protected virtual void OnHit(BulletInfo info)
        {
        }
    }
}
