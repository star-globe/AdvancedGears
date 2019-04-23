using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class HitNotifier : MonoBehaviour
    {
        public event Action<Collision> OnCollisionEvent;
        public event Action<Collider> OnColliderEvent;

        private void OnCollisionEnter(Collision collision)
        {
            OnCollisionEvent?.Invoke(collision);
        }

        private void OnTriggerEnter(Collider other)
        {
            OnColliderEvent?.Invoke(other);
        }
    }
}
