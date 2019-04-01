using System;
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
        readonly List<Rigidbody> activeBullets = new List<Rigidbody>();
        readonly Queue<Rigidbody> deactiveQueue = new Queue<Rigidbody>();

        readonly Dictionary<long,Action<ulong>> entityDic = new Dictionary<long,Action<ulong>>();

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
            //archetype = entityManager.CreateArchetype(typeof(SphereCollider), typeof(BulletInfo));
        }

        public void RegisterTriggerEntityId(EntityId entityId, Action<ulong> action)
        {
            long id = entityId.Id;
            if (entityDic.ContainsKey(id))
                entityDic[id] = action;
            else
                entityDic.Add(id, action);
        }

        public void RemoveTriggerEntity(EntityId entityId)
        {
            entityDic.Remove(entityId.Id);            
        }

        public void InvokeVanishAction(long entity_id, ulong bullet_id)
        {
            if (entityDic.ContainsKey(entity_id))
                entityDic[entity_id](bullet_id);
        }

        public void OnFire(BulletFireInfo info)
        {
            if (BulletObject == null || entityManager == null)
                return;

            // check
            Rigidbody bullet;
            if (deactiveQueue.Count > 1)
            {
                bullet = deactiveQueue.Dequeue();
            }
            else
            {
                var go = Instantiate(BulletObject);
                bullet = go.GetComponent<Rigidbody>();
                activeBullets.Add(bullet);
            }

            bullet.gameObject.SetActive(true);
            bullet.enabled = true;
            bullet.useGravity = true;
            bullet.isKinematic = false;
            bullet.detectCollisions = entityDic.ContainsKey(info.ShooterEntityId); 

            bullet.position = new Vector3(info.LaunchPosition.X, info.LaunchPosition.Y, info.LaunchPosition.Z);

            var vec = new Vector3(info.InitialVelocity.X, info.InitialVelocity.Y, info.InitialVelocity.Z);
            bullet.gameObject.transform.forward = vec.normalized;
            bullet.velocity = vec;

            var fireComponent = bullet.GetComponent<BulletFireComponent>();
            fireComponent.Value = new BulletInfo(info);
            //var entity = entityManager.CreateEntity();// archetype);
            //objectEntity.CopyAllComponentsToEntity(entityManager, entity);

            //objectEntity.CopyAllComponentsToEntity(entityManager, entity);
            //entityManager.SetComponentData(entity, new BulletInfo(info));
        }
    }

}
