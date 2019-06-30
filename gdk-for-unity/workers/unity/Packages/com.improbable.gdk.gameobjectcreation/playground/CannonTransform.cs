using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class CannonTransform : AttachedTransform
    {
        [SerializeField] Transform barell;
        public Transform Barrell { get { return barell; } }

        [SerializeField] Transform muzzle;
        public Transform Muzzle { get { return muzzle; } }

        void Start()
        {
            Assert.IsNotNull(barell);
            Assert.IsNotNull(muzzle);
        }

        public Vector3 Forward { get { return barell.forward; } }
    }
}
