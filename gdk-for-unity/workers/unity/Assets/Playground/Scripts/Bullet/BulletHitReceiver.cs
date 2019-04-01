using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class BulletHitReceiver : BulletFireBase
    {
        [Require] World world;
        protected override World World => world;

        const string bulletTag = "Bullet";

        private void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag != bulletTag)
                return;

            var fire = other.gameObject.GetComponent<BulletFireComponent>();
            if (fire == null)
                return;

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
    }
}
