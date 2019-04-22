using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class UnitTransform : MonoBehaviour
    {
        [SerializeField] CannonTransform cannon;
        public CannonTransform Cannon { get { return cannon; } }

        [SerializeField] Rigidbody vehicle;
        public Rigidbody Vehicle { get { return vehicle; }}

        void Start()
        {
            Assert.IsNotNull(cannon);
            Assert.IsNotNull(vehicle);
        }
    }
}
