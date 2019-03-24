using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    internal class BulletCreator : MonoBehaviour
    {
        [Require] ClientBulletComponentReader clientReader;
        [Require] WorkerBulletComponentReader workerReader;

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
        LinkedEntityComponent spatialOSComponent;

        readonly List<GameObject> activeBullets = new List<GameObject>();
        readonly Queue<GameObject> deactiveQueue = new Queue<GameObject>();

        float checkTime = 0.0f;
        const float interval = 15.0f;

        private void OnEnable()
        {
            clientReader.OnFiresEvent += OnFire;
            workerReader.OnFiresEvent += OnFire;
        }

        private void Start()
        {
            spatialOSComponent = GetComponent<LinkedEntityComponent>();
            entityManager = spatialOSComponent.World.GetOrCreateManager<EntityManager>();
        }

        private void Update()
        {
            if (Time.realtimeSinceStartup - checkTime < interval)
                return;

            activeBullets.RemoveAll(b =>
            {
                if (b == null || b.Equals(null))
                    return true;

                if (!b.activeSelf)
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
        }

        void OnFire(BulletFireInfo info)
        {
            if (BulletObject == null)
                return;

            // check
            GameObject bullet;
            if (deactiveQueue.Count > 1)
            {
                bullet = deactiveQueue.Dequeue();
            }
            else
            {
                bullet = Instantiate(BulletObject);
                activeBullets.Add(bullet);
            }

            bullet.SetActive(true);
            var entity = bullet.GetComponent<GameObjectEntity>();

            entityManager.AddComponentData(entity.Entity, new BulletInfo(info));
        }
    }

}
