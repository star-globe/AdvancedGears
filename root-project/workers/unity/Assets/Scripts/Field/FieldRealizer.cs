using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class FieldRealizer : MonoBehaviour
    {
        [SerializeField]
        BoxCollider boxCollider;

        private void Start()
        {
            Assert.IsNotNull(boxCollider);
        }

        public void Realize(float size, Vector3 pos)
        {
            this.transform.position = pos;
            float rate = size / boxCollider.size.x;
            this.transform.localScale = new Vector3(rate, 1.0f, rate);
        }
    }
}
