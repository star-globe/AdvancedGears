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
        [SerializeField] Transform turret;
        public Transform Turret { get { return turret; } }

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
    }
}
