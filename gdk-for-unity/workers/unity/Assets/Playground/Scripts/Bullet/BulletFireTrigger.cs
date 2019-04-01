using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable;

namespace Playground
{
    public class BulletFireTrigger : BulletFireBase
    {
        [Require] BulletComponentWriter writer;
        [Require] World world;

        protected override World World => world;

        [SerializeField]
        Transform muzzleTransform;

        [SerializeField]
        float bulletSpeed = 0.5f;

        [SerializeField]
        float lifeTime = 2.0f;

        [SerializeField]
        float interval = 0.4f;

        float fireTime = 0.0f;

        LinkedEntityComponent spatialComp = null;
        LinkedEntityComponent SpatialComp
        {
            get
            {
                if (spatialComp == null)
                    spatialComp = GetComponent<LinkedEntityComponent>();

                return spatialComp;
            }
        }

        public bool IsAvailable { get { return writer != null; } }

        private void Start()
        {
            Assert.IsNotNull(muzzleTransform);
            base.Creator.RegisterTriggerEntityId(this.SpatialComp.EntityId, VanishBullet);
        }

        private void OnDestroy()
        {
            base.Creator.RemoveTriggerEntity(this.SpatialComp.EntityId);
        }

        public void OnFire()
        {
            if (this.SpatialComp == null || writer == null)
                return;

            var time = Time.realtimeSinceStartup;
            if (time - fireTime <= interval)
                return;

            fireTime = time;

            var pos = muzzleTransform.position;
            var vec = muzzleTransform.forward;
            vec *= bulletSpeed;

            var id = writer.Data.CurrentId;
            var fire = new BulletFireInfo()
            {
                Power = 1,
                Type = 1,
                Alignment = 3,
                LaunchPosition = new Vector3f(pos.x, pos.y, pos.z),
                InitialVelocity = new Vector3f(vec.x, vec.y, vec.z),
                LaunchTime = Time.realtimeSinceStartup,
                LifeTime = lifeTime,
                GunId = 0,
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            writer.SendUpdate(new BulletComponent.Update
            {
                CurrentId = id + 1
            });
            writer.SendFiresEvent(fire);
        }

        private void VanishBullet(ulong id)
        {
            var vanish = new BulletVanishInfo()
            {
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            writer.SendVanishesEvent(vanish);
        }
    }
}
