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

        [SerializeField] BoxCollider boxDetect;
        [SerializeField] SphereCollider sphereDetect;

        private Collider Detect
        {
            get
            {
                if (boxDetect != null)
                    return boxDetect;

                if (sphereDetect != null)
                    return sphereDetect;

                return null;
            }
        }

        public Bounds Bounds
        {
            get
            {
                return Detect.bounds;
            }
        }

        public Vector3 BufferVector
        {
            get
            {
                if (this.Detect == null)
                    return this.transform.up;

                var up = this.Detect.transform.up;
                var diff = this.transform.position - this.Detect.bounds.center;
                var dot = Vector3.Dot(up,diff);
                return up * (dot+this.Detect.bounds.extents.y);
            }
        }

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

        //public PostureTransform GetPosture(PosturePoint point)
        //{
        //    PostureTransform posture = null;
        //    this.PostureDic.TryGetValue(point, out posture);
        //    return posture;
        //}

        //Dictionary<PosturePoint,CannonTransform> cannonDic = null;
        //public CannonTransform GetCannonTransform(PosturePoint point)
        //{
        //    cannonDic = cannonDic ?? new Dictionary<PosturePoint,CannonTransform>();
        //    if (cannonDic.ContainsKey(point) == false)
        //    {
        //        var cannon = this.GetTerminal<CannonTransform>(point);
        //        if (cannon == null)
        //            return null;
        //
        //        cannonDic.Add(point, cannon);
        //    }
        //
        //    return cannonDic[point];
        //}

        //public void Clear()
        //{
        //    postureDic?.Clear();
        //    cannonDic?.Clear();
        //}

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

        public bool IsGrounded { get; private set; }

        Ray vertical = new Ray();
        int? layer = null;
        int Layer
        {
            get
            {
                layer = layer ?? LayerMask.GetMask("Ground", "UnitObject");
                return layer.Value;
            }
        }
        
        public bool GetGrounded()
        {
            return GetGrounded(out var hitInfo);
        }

        public bool GetGrounded(out RaycastHit hitInfo)
        {
            if (this.Detect == null) {
                hitInfo = new RaycastHit();
                return false;
            }

            var bounds = this.Detect.bounds;
            vertical.direction = -this.Detect.transform.up;
            vertical.origin = bounds.center;
            return Physics.Raycast(vertical, out hitInfo, bounds.extents.y * 1.1f, this.Layer);
        }

        public Vector3 GetUpAxis()
        {
            if (this.Detect == null)
                return Vector3.up;

            return this.Detect.transform.up;
        }

#if false
        private void OnCollisionStay(Collision collision)
        {
            CheckConstacts(collision.contacts, true);
        }

        private void OnCollisionExit(Collision collision)
        {
            CheckConstacts(collision.contacts, false);
        }

        private void CheckConstacts(ContactPoint[] points, bool inOut)
        {
            foreach (var p in points)
            {
                if (Vector3.Dot(p.normal, groundDetect.transform.up) <= 0)
                    continue;

                IsGrounded = inOut;
            }
        }
   #endif
    }
}
