using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class UnitTransform : MonoBehaviour
    {
        [SerializeField] PostureTransform[] postures;

        [SerializeField] Rigidbody vehicle;
        public Rigidbody Vehicle { get { return vehicle; }}

        [SerializeField] BoxCollider groundDetect;
        public BoxCollider GroundDetect { get { return groundDetect; } }

        Dictionary<PosturePoint,PostureTransform> postureDic = null;
        public Dictionary<PosturePoint,PostureTransform> PostureDic
        {
            get
            {
                if (postureDic == null)
                {
                    postureDic = new Dictionary<PosturePoint, PostureTransform>();
                    foreach(var p in postures)
                    {
                        if (p == null && postureDic.ContainsKey(p.Point))
                            continue;

                        postureDic.Add(p.Point,p);
                    }
                }

                return postureDic;
            }
        }
        public PosturePoint[] GetKeys()
        {
            return this.PostureDic.Keys.ToArray();
        }

        public PostureTransform GetPosture(PosturePoint point)
        {
            PostureTransform posture = null;
            this.PostureDic.TryGetValue(point, out posture);
            return posture;
        }

        Dictionary<PosturePoint,CannonTransform> cannonDic = null;
        public CannonTransform GetCannonTransform(PosturePoint point)
        {
            cannonDic = cannonDic ?? new Dictionary<PosturePoint,CannonTransform>();
            if (cannonDic.ContainsKey(point) == false)
            {
                var cannon = this.GetTerminal<CannonTransform>(point);
                if (cannon == null)
                    return null;

                cannonDic.Add(point, cannon);
            }

            return cannonDic[point];
        }

        public void Clear()
        {
            postureDic?.Clear();
            cannonDic?.Clear();
        }

        void Start()
        {
            Assert.IsNotNull(vehicle);
        }

        public T GetTerminal<T>(PosturePoint point) where T : AttachedTransform
        {
            if (this.PostureDic.ContainsKey(point))
                return this.PostureDic[point].TerminalAttached as T;

            return null;
        }

        public void SetQuaternion(PosturePoint point, int index, UnityEngine.Quaternion quo)
        {
            if (this.PostureDic.ContainsKey(point) == false)
                return;

            this.PostureDic[point].SetQuaternion(index, quo);
        }
    }
}
