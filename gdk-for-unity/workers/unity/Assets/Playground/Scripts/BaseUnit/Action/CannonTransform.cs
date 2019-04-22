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

        [SerializeField] Tranform barell;
        public Transform Barrell { get { return barell; } }

        [SerializeField] Tranform muzzle;
        public Transform Muzzle { get { return muzzle; } }
        void Start()
        {
            Assert.IsNotNull(turret);
            Assert.IsNotNull(barell);
            Assert.IsNotNull(muzzle);
        }
    }
}
