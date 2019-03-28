using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable;

namespace Playground
{
    public class ClientShootings : BaseShootings
    {
        [Require] ClientBulletComponentWriter writer;

        protected override void SetFireEvent(BulletFireInfo fire)
        {
            writer.SendFiresEvent(fire);
        }
    }

    public abstract class BaseShootings : MonoBehaviour
    {
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

        [Require] ClientBulletComponentWriter writer;

        private void Start()
        {
            Assert.IsNotNull(muzzleTransform);
        }

        public void OnFire()
        {
            if (this.SpatialComp == null)
                return;

            var time = Time.realtimeSinceStartup;
            if (time - fireTime <= interval)
                return;

            fireTime = time;

            var pos = muzzleTransform.position;
            var vec = muzzleTransform.forward;
            vec *= bulletSpeed;

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
            };

            SetFireEvent(fire);
        }

        protected abstract void SetFireEvent(BulletFireInfo fire);
    }
}
