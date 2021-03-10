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
    public delegate void BulletVanishEvent(uint type, ulong bullet_id);

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

        class BulletsContainer
        {
            public BulletsContainer(uint type, Transform parent, Vector3 origin)
            {
                this.typeId = type;
                bulletParent = parent;
                this.Origin = origin;
            }

            Vector3 Origin;
            Transform bulletParent; 
            public uint typeId { get; private set;}

            GameObject bulletObject = null;
            GameObject BulletObject
            {
                get
                {
                    if (bulletObject == null) {
                        var settings = BulletDictionary.Get(this.typeId);
                        if (settings != null)
                            bulletObject = settings.BulletModel;
                    }

                    return bulletObject;
                }
            }

            readonly Queue<Rigidpair> deactiveQueue = new Queue<Rigidpair>();
            readonly Dictionary<long, Dictionary<ulong, Rigidpair>> bulletsDic = new Dictionary<long, Dictionary<ulong, Rigidpair>>();
            readonly List<ulong> removeKeyList = new List<ulong>();

            public void Update()
            {
                foreach (var dic in bulletsDic) {
                    removeKeyList.Clear();
                    foreach (var kvp in dic.value) {
                        if (kvp.Value.IsActive == false)
                            removeKeyList.Add(kvp.Key);
                    }

                    foreach(var r in removeKeyList) {
                        deactiveQueue.Enqueue(dic.Value[r]);
                        dic.Value.Remove(r);
                    }
                }
            }

            public void OnFire(BulletFireInfo info, bool detectCollisions)
            {
                // check
                Rigidpair bullet;
                if (deactiveQueue.Count > 1) {
                    bullet = deactiveQueue.Dequeue();
                }
                else {
                    var go = Instantiate(this.BulletObject, this.bulletParent);
                    bullet = new Rigidpair(go.GetComponent<Rigidbody>());
                }

                bullet.IsActive = true;
                bullet.Rigid.useGravity = true;
                bullet.Rigid.isKinematic = false;
                bullet.Rigid.detectCollisions = detectCollisions;

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
                if (bulletsDic.ContainsKey(key)) {
                    var dic = bulletsDic[key];
                    if (dic.ContainsKey(id))
                        dic[id] = bullet;
                    else
                        dic.Add(id, bullet);
                }
                else {
                    var dic = new Dictionary<ulong, Rigidpair>();
                    dic.Add(id, bullet);
                    bulletsDic.Add(key, dic);
                }
            }

            public void OnVanish(BulletVanishInfo info)
            {
                if (bulletsDic.TryGetValue(info.ShooterEntityId, out var dic) == false)
                    return;

                if (dic.TryGetValue(info.BulletId, out var bullet) == false)
                    return;

                var fireComponent = bullet.Fire;
                var b = fireComponent.Value;
                fireComponent.Value = new BulletInfo(b,0);
                bullet.IsActive = false;
            }
        }

        EntityManager entityManager;
        Vector3 Origin;

        readonly Dictionary<uint,BulletsContainer> containerDic = new Dictionary<uint,BulletsContainer>();
        readonly Dictionary<long, (GameObject,BulletVanishEvent)> entityDic = new Dictionary<long,(GameObject,BulletVanishEvent)>();

        float checkTime = 0.0f;
        const float interval = 3.0f;

        private void Update()
        {
            if (Time.time - checkTime < interval)
                return;

            foreach (var kvp in containerDic)
                kvp.Value.Update();
        }

        public void Setup(EntityManager entity, Vector3 origin)
        {
            entityManager = entity;
            this.Origin = origin;
        }

        public void RegisterTriggerEntityId(EntityId entityId, (GameObject,BulletVanishEvent) pair)
        {
            long id = entityId.Id;
            if (entityDic.ContainsKey(id))
                entityDic[id] = pair;
            else
                entityDic.Add(id, pair);
        }

        public void RemoveTriggerEntity(EntityId entityId)
        {
            entityDic.Remove(entityId.Id);            
        }

        public void InvokeVanishAction(long entity_id, uint type, ulong bullet_id)
        {
            if (entityDic.ContainsKey(entity_id) == false)
                return;

            var pair = entityDic[entity_id];
            if (pair.Item1 == null || pair.Item1.Equals(null))
                return;

            pair.Item2(type, bullet_id);
        }

        public void OnFire(BulletFireInfo info)
        {
            if (entityManager == null)
                return;

            BulletsContainer container;
            var type = info.Type;
            if (containerDic.TryGetValue(type, out container) == false) {
                container = new BulletsContainer(type, this.transform,  this.Origin);
                containerDic.Add(type, container);
            }

            container.OnFire(info, entityDic.ContainsKey(info.ShooterEntityId));
        }

        public void OnVanish(BulletVanishInfo info)
        {
            var type = info.Type;
            if (containerDic.TryGetValue(type, out var container) == false)
                return;

            container.OnVanish(info);
        }
    }

}
