using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class CannonTransform : MonoBehaviour
    {
        [SerializeField] Rigidbody turret;
        public Rigidbody Turret { get { return turret; } }

        [SerializeField] Transform barell;
        public Transform Barrell { get { return barell; } }

        [SerializeField] Transform muzzle;
        public Transform Muzzle { get { return muzzle; } }

        void Start()
        {
            Assert.IsNotNull(turret);
            Assert.IsNotNull(barell);
            Assert.IsNotNull(muzzle);
        }

        public Vector3 Forward { get { return barell.forward; } }

        public static void Rotate(CannonTransform cannon, Vector3 foward, float angle)
        {
            var trans = cannon.Turret.transform;
            var dot = Vector3.Dot(trans.up,foward);
            foward -= dot * trans.up;
            foward.Normalize();

            var axis = Vector3.Cross(trans.forward, foward);
            var ang = Vector3.Angle(trans.forward, foward);
            if (ang < angle)
                angle = ang;

            var q = Quaternion.AngleAxis(angle, axis.normalized);
            var nq = trans.rotation * q;
            cannon.Turret.MoveRotation(nq);
        }
    }
}
