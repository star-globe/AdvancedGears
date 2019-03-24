using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk.GameObjectRepresentation;

namespace Playground
{
    internal class BulletCreator : MonoBehaviour
    {
        EntityManager entityManager;

        [Require] BulletComponent.Requirable.Reader reader;

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

        readonly List<GameObject> activeBullets = new List<GameObject>();
        readonly Queue<GameObject> deactiveQueue = new Queue<GameObject>();

        float checkTime = 0.0f;
        const float interval = 15.0f;

        private void Start()
        {
            //Assert.IsNotNull(bulletObject);
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
            reader.OnFires += OnFire;
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
