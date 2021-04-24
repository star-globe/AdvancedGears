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
        PostureBoneContainer container;

        protected CannonTransform GetMuzzleTransform(int bone)
        {
            return container.GetCannon(bone);
        }

        Dictionary<int, float> fireTimeDic = null;

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
            Assert.IsNotNull(container);
            base.Creator?.RegisterTriggerEntityId(this.SpatialComp.EntityId, (this.gameObject, VanishBullet));
        }

        protected virtual void OnEnable()
        {
            origin = World.GetExistingSystem<WorkerSystem>().Origin;
        }
        
        private void OnDestroy()
        {
            base.Creator?.RemoveTriggerEntity(this.SpatialComp.EntityId);
        }

        public void OnFire(int bone, uint gunId)
        {
            if (this.SpatialComp == null || this.BulletWriter == null)
                return;

            var gun = GunDictionary.GetGunSettings(gunId);
            if (gun == null)
                return;

            var time = Time.time;
            fireTimeDic = fireTimeDic ?? new Dictionary<int, float>();

            float fireTime = 0.0f;
            fireTimeDic.TryGetValue(bone, out fireTime);

            if (time - fireTime <= gun.Inter)
                return;

            var cannon = GetMuzzleTransform(bone);
            if (cannon == null)
                return;

            fireTimeDic[bone] = time;

            var pos = cannon.Muzzle.position - origin;
            var vec = cannon.Forward;
            vec *= gun.BulletSpeed;

            var id = this.BulletWriter.Data.CurrentId;
            var fire = new BulletFireInfo()
            {
                Power = 1,
                Type = gun.BulletTypeId,
                Alignment = 3,
                LaunchPosition = pos.ToFixedPointVector3(),
                InitialVelocity = vec.ToFixedPointVector3(),
                LaunchTime = Time.time,
                LifeTime = gun.BulletLifeTime,
                GunId = gunId,
                ShooterEntityId = SpatialComp.EntityId.Id,
                BulletId = id,
            };

            this.BulletWriter.SendUpdate(new BulletComponent.Update
            {
                CurrentId = id + 1
            });
            this.BulletWriter.SendFiresEvent(fire);
        }

        private void VanishBullet(uint type, ulong id)
        {
            if (this.BulletWriter == null)
                return;

            var vanish = new BulletVanishInfo()
            {
                ShooterEntityId = SpatialComp.EntityId.Id,
                Type = type,
                BulletId = id,
            };

            this.BulletWriter.SendVanishesEvent(vanish);
        }
    }
}
