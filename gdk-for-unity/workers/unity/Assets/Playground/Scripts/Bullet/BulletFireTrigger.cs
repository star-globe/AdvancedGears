using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable;
using Improbable.Gdk.Core;

namespace Playground
{
    public class BulletFireTrigger : BulletFireBase
    {
        [Require] BaseUnitActionReader actionReader;
        [Require] BulletComponentWriter bulletWriter;
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

        private Vector3 origin;
        private Vector3 target = Vector3.zero;

        public bool IsAvailable { get { return bulletWriter != null; } }

        private void Start()
        {
            Assert.IsNotNull(muzzleTransform);
            base.Creator.RegisterTriggerEntityId(this.SpatialComp.EntityId, VanishBullet);
        }

        private void OnEnable()
        {
            if (actionReader != null)
                actionReader.OnFireTriggeredEvent += OnTarget;

            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }
        
        private void OnDestroy()
        {
            if (this.Creator != null)
                base.Creator.RemoveTriggerEntity(this.SpatialComp.EntityId);
        }

        private void OnTarget(AttackTargetInfo info)
        {
            // rotate to target
            if (target.x != info.TargetPosition.X ||
                target.y != info.TargetPosition.Y ||
                target.z != info.TargetPosition.Z)
            {
                target.Set( info.TargetPosition.X,
                            info.TargetPosition.Y,
                            info.TargetPosition.Z);
            }
            
            var diff = target + origin - muzzleTransform.position;
            muzzleTransform.forward = diff.normalized;
            OnFire();
        }

        public void OnFire()
        {
            if (this.SpatialComp == null || bulletWriter == null)
                return;

            var time = Time.realtimeSinceStartup;
            if (time - fireTime <= interval)
                return;

            fireTime = time;

            var pos = muzzleTransform.position - origin;
            var vec = muzzleTransform.forward;
            vec *= bulletSpeed;

            var id = bulletWriter.Data.CurrentId;
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

            bulletWriter.SendUpdate(new BulletComponent.Update
            {
                CurrentId = id + 1
            });
            bulletWriter.SendFiresEvent(fire);
        }

        private void VanishBullet(ulong id)
        {
            var vanish = new BulletVanishInfo()
            {
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            bulletWriter.SendVanishesEvent(vanish);
        }
    }
}
