using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;

namespace AdvancedGears
{
    public class BulletFireTrigger : BulletFireTriggerBase
    {
        [Require] BulletComponentWriter bulletWriter;
        [Require] World world;

        protected override BulletComponentWriter BulletWriter => bulletWriter;
        protected override World World => world;
    }

    public abstract class BulletFireTriggerBase : BulletFireBase
    {
        protected abstract BulletComponentWriter BulletWriter { get; }

        [SerializeField]
        UnitTransform unitTransform;

        protected Transform GetMuzzleTransform(PosturePoint point)
        {
            var cannon = unitTransform.GetCannonTransform(point);
            return cannon == null ? null: cannon.Muzzle;
        }

        [SerializeField]
        float bulletSpeed = 4.5f;

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

        protected Vector3 origin { get; private set; }

        public bool IsAvailable { get { return this.BulletWriter != null; } }

        private void Start()
        {
            Assert.IsNotNull(unitTransform);
            base.Creator?.RegisterTriggerEntityId(this.SpatialComp.EntityId, VanishBullet);
        }

        protected virtual void OnEnable()
        {
            origin = World.GetExistingSystem<WorkerSystem>().Origin;
        }
        
        private void OnDestroy()
        {
            base.Creator?.RemoveTriggerEntity(this.SpatialComp.EntityId);
        }

        public void OnFire(PosturePoint point)
        {
            if (this.SpatialComp == null || this.BulletWriter == null)
                return;

            var time = Time.time;
            if (time - fireTime <= interval)
                return;

            var muzzle = GetMuzzleTransform(point);
            if (muzzle == null)
                return;

            fireTime = time;

            var pos = muzzle.position - origin;
            var vec = muzzle.forward;
            vec *= bulletSpeed;

            var id = this.BulletWriter.Data.CurrentId;
            var fire = new BulletFireInfo()
            {
                Power = 1,
                Type = 1,
                Alignment = 3,
                LaunchPosition = pos.ToFixedPointVector3(),
                InitialVelocity = vec.ToFixedPointVector3(),
                LaunchTime = Time.time,
                LifeTime = lifeTime,
                GunId = 0,
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            this.BulletWriter.SendUpdate(new BulletComponent.Update
            {
                CurrentId = id + 1
            });
            this.BulletWriter.SendFiresEvent(fire);
        }

        private void VanishBullet(ulong id)
        {
            var vanish = new BulletVanishInfo()
            {
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            this.BulletWriter.SendVanishesEvent(vanish);
        }
    }
}
