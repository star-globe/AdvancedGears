using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class PostureRealizer : MonoBehaviour
    {
        [Require] BaseUnitPostureReader reader;

        [SerializeField] UnitTransform unit;

        private void Start()
        {
            Assert.IsNotNull(unit);
        }

        private void OnEnable()
        {
            reader.OnPostureChangedEvent += PostureChanged;
        }

        void PostureChanged(PostureData data)
        {
            if (data.Point == PosturePoint.Bust)
            {
                SetTransform(unit.Cannon.Turret.transform, data.Rotation);
            }
        }

        void SetTransform(Transform trans, Improbable.Transform.Quaternion quo)
        {
            var q = new Quaternion(quo.X, quo.Y, quo.Z, quo.W);
            trans.rotation = q;
        }
    }
}
