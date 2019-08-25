using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class BulletCreator : MonoBehaviour
    {
        class Rigidpair
        {
            public Rigidbody Rigid { get; private set;}
            public BulletFireComponent Fire { get; private set;} 

            public bool IsActive
            {
                get { return Rigid.gameObject.activeSelf; }
                set
                {
                    Rigid.gameObject.SetActive(value);                    
                }
            }

            public Rigidpair(Rigidbody rig)
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
        Vector3 Origin;

        readonly Queue<Rigidpair> deactiveQueue = new Queue<Rigidpair>();

        readonly Dictionary<long, Dictionary<ulong, Rigidpair>> bulletsDic = new Dictionary<long, Dictionary<ulong, Rigidpair>>();

        readonly Dictionary<long,Action<ulong>> entityDic = new Dictionary<long,Action<ulong>>();

        float checkTime = 0.0f;
        const float interval = 3.0f;

        private void Update()
        {
            if (Time.time - checkTime < interval)
                return;

            foreach (var dic in bulletsDic)
            {
                var removeKeys = dic.Value.Where(kvp => !kvp.Value.IsActive).Select(kvp => kvp.Key).ToArray();
                foreach(var r in removeKeys)
                {
                    deactiveQueue.Enqueue(dic.Value[r]);
                    dic.Value.Remove(r);
                }
            }
        }

        public void Setup(EntityManager entity, Vector3 origin)
        {
            entityManager = entity;
            this.Origin = origin;
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
            }

            bullet.IsActive = true;
            bullet.Rigid.useGravity = true;
            bullet.Rigid.isKinematic = false;
            bullet.Rigid.detectCollisions = entityDic.ContainsKey(info.ShooterEntityId);

            var pos = info.LaunchPosition.ToUnityVector();
            pos += Origin;
            bullet.Rigid.position = pos;
            bullet.Rigid.transform.position = pos;

            var vec = info.InitialVelocity.ToUnityVector();;
            bullet.Rigid.transform.forward = vec.normalized;
            bullet.Rigid.velocity = vec;
            bullet.Rigid.angularVelocity = Vector3.zero;
            bullet.Fire.Value = new BulletInfo(info);

            // add
            var key = info.ShooterEntityId;
            var id = info.BulletId;
            if (bulletsDic.ContainsKey(key))
            {
                var dic = bulletsDic[key];
                if (dic.ContainsKey(id))
                    dic[id] = bullet;
                else
                    dic.Add(id, bullet);
            }
            else
            {
                var dic = new Dictionary<ulong, Rigidpair>();
                dic.Add(id, bullet);
                bulletsDic.Add(key, dic);
            }
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
            bullet.IsActive = false;
        }
    }

}
