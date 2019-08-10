using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class BaseUnitRigidbodyInitializer : MonoBehaviour
    {
        [SerializeField]
        Rigidbody rigidBody;

        [SerializeField]
        RigidbodyConfigRef configRef;

        private void Start()
        {
            Assert.IsNotNull(rigidBody);
            Assert.IsNotNull(configRef);

            configRef.RigidbodySettings.SetRigid(rigidBody);
        }
    }
}
