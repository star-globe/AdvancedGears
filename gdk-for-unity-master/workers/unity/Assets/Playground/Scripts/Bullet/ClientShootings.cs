using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.GameObjectRepresentation;
using Improbable;

namespace Playground
{
    public class ClientShootings : MonoBehaviour
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

        SpatialOSComponent spatialComp = null;
        SpatialOSComponent SpatialComp
        {
            get
            {
                if (spatialComp == null)
                    spatialComp = GetComponent<SpatialOSComponent>();

                return spatialComp;                
            }
        }

        [Require] BulletComponent.Requirable.Writer writer;

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
                ShooterEntityId = SpatialComp.SpatialEntityId.Id,
            };

            writer.SendFires(fire);
        }
    }
}
