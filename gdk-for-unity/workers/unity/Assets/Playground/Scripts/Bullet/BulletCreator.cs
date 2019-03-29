using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class BulletCreator : MonoBehaviour
    {
        GameObject bulletObject = null;
        GameObject BulletObject
        {
            get
            {
                if (bulletObject == null)
                {
                    var settings = BulletDictionary.Get(0);
                    if (settings != null)
                        bulletObject = settings.BulletModel;
                }

                return bulletObject;
            }
        }

        EntityManager entityManager;
        EntityArchetype archetype;
        readonly List<Collider> activeBullets = new List<Collider>();
        readonly Queue<Collider> deactiveQueue = new Queue<Collider>();

        float checkTime = 0.0f;
        const float interval = 15.0f;

        private void Update()
        {
            if (Time.realtimeSinceStartup - checkTime < interval)
                return;

            activeBullets.RemoveAll(b =>
            {
                if (b == null || b.Equals(null))
                    return true;

                if (!b.enabled)
                {
                    deactiveQueue.Enqueue(b);
                    return true;
                }

                return false;
            });
        }

        public void Setup(EntityManager entity)
        {
            entityManager = entity;
            archetype = entityManager.CreateArchetype(typeof(Collider), typeof(BulletInfo));
        }

        public void OnFire(BulletFireInfo info)
        {
            if (BulletObject == null || entityManager == null)
                return;

            // check
            Collider bullet;
            if (deactiveQueue.Count > 1)
            {
                bullet = deactiveQueue.Dequeue();
            }
            else
            {
                var go = Instantiate(BulletObject);
                bullet = go.GetComponent<Collider>();
                activeBullets.Add(bullet);
            }

            bullet.enabled = true;
            bullet.gameObject.SetActive(true);
            var objectEntity = bullet.GetComponent<GameObjectEntity>();

            var entity = entityManager.CreateEntity(archetype);
            objectEntity.CopyAllComponentsToEntity(entityManager, entity);
            entityManager.AddComponentData(entity, new BulletInfo(info));
        }
    }

}
