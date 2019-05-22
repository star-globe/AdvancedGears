using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
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
            return this.postureDic.Keys();
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

        public List<Improbable.Transform.Quaternion> GetAllRotates(PosturePoint point)
        {
            if (this.PostureDic.ContainsKey(point) == false)
                return new List<Improbable.Transform.Quaternion>();

            return this.PostureDic[point].Connectors.Select(c => c.transform.rotation.ToImprobableQuaternion()).ToList();
        }

        public void SetQuaternion(PosturePoint point, int index, Quaternion quo)
        {
            if (this.PostureDic.ContainsKey[point] == false)
                return;

            this.PostureDic[point].SetQuaternion(index, quo);
        }
    }
}
