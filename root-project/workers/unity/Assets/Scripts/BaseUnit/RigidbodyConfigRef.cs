using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class RigidbodyConfigRef : MonoBehaviour
    {
        [SerializeField]
        RigidbodySettings rigidSettings;
        public RigidbodySettings RigidbodySettings => rigidSettings;

        private void Start()
        {
            Assert.IsNotNull(rigidSettings);
        }
    }
}

