using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Core;

namespace Playground
{
    public class BulletCreator : MonoBehaviour
    {
        class Rigidpair
        {
            public Rigidbody Rigid { get; private set;}
            public BulletFireComponent Fire { get; private set;} 

            public bool IsActive
            {
                get { return Rigid.activeSelf; }
                set
                {
                    Rigid.gameObject.SetActive(value);                    
                }
            }

            public Rigpair(Rigidbody rig)
            {
                if (rig == null)
                    return;

                this.Rigid = rig;
                this.Fire = rig.gameObject.GetComponent<BulletFireComponent>();
            }
        }

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
        //readonly List<Rigidpair> activeBullets = new List<Rigidpair>();
        readonly Queue<Rigidpair> deactiveQueue = new Queue<Rigidpair>();

        readonly Dictionary<long, Dictionary<ulong, Rigidpair>> bulletsDic = new Dictionary<long, Dictionary<ulong, Rigidbody>>();

        readonly Dictionary<long,Action<ulong>> entityDic = new Dictionary<long,Action<ulong>>();

        float checkTime = 0.0f;
        const float interval = 15.0f;

        private void Update()
        {
            if (Time.realtimeSinceStartup - checkTime < interval)
                return;

            foreach (var dic in bulletsDic)
            {
                var removeKeys = dic.Value.Where(kvp => !kvp.Value.IsActive).Select(kvp => kvp.Key).ToArray();
                foreach(var r in removeKeys)
                {
                    deactiveQueue.Enqueue(dic[r]);
                    dic.Remove(r);   
                }
            }
            //activeBullets.RemoveAll(b =>
            //{
            //    if (b == null || b.Equals(null))
            //        return true;
            //
            //    if (!b.gameObject.activeSelf)
            //    {
            //        deactiveQueue.Enqueue(b);
            //        return true;
            //    }
            //
            //    return false;
            //});
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
            Rigidpair bullet;
            if (deactiveQueue.Count > 1)
            {
                bullet = deactiveQueue.Dequeue();
            }
            else
            {
                var go = Instantiate(BulletObject);
                bullet = new Rigidpair(go.GetComponent<Rigidbody>());
                var key = info.ShooterEntityId;
                var id = info.BulletId;
                if (bulletsDic.ContainsKey(key))
                {
                    var dic = bulletsDic[key];
                    if (dic.ContainsKey(id))
                        dic[id] = bullet;
                    else
                        dic.Add(id,bullet);
                }
                else
                {
                    var dic = new Dictionary<ulong,Rigidpair>();
                    dic.Add(id,bullet);
                    bulletDic.Add(key, dic);
                }
            }

            bullet.IsActive = true;
            bullet.Rigid.useGravity = true;
            bullet.Rigid.isKinematic = false;
            bullet.Rigid.detectCollisions = entityDic.ContainsKey(info.ShooterEntityId); 

            bullet.Rigid.position = new Vector3(info.LaunchPosition.X, info.LaunchPosition.Y, info.LaunchPosition.Z);

            var vec = new Vector3(info.InitialVelocity.X, info.InitialVelocity.Y, info.InitialVelocity.Z);
            bullet.Rigid.gameObject.transform.forward = vec.normalized;
            bullet.Rigid.velocity = vec;

            //var fireComponent = bullet.GetComponent<BulletFireComponent>();
            bullet.Fire.Value = new BulletInfo(info);
        }

        public void OnVanish(BulletVanishInfo info)
        {
            Dictionary<ulong, Rigidpair> dic;
            if (bulletsDic.TryGetValue(info.ShooterEntityId, out dic) == false)
                return;

            Rigidpair bullet;
            if (dic.TryGetValue(info.BulletId, out bullet) == false)
                return;

            var fireComponent = bullet.Fire;
            var b = fireComponent.Value;
            fireComponent.Value = new BulletInfo(b,0);
        }
    }

}
