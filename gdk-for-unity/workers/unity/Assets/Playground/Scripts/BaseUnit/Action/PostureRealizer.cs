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

        Transform Turret
        {
            get{ return unit.Cannon.Turret; }
        }

        private void Start()
        {
            Assert.IsNotNull(unit);
        }

        private void OnEnable()
        {
            reader.OnPostureChangedEvent += PostureChanged;

            // initialize
            var data = reader.Data;
            this.transform.rotation = data.Root.ToUnityQuaternion();

            PostureData pos;
            if (data.Posture.Datas.TryGetValue(PosturePoint.Bust, out pos))
            {
                this.Turret.rotation = pos.Rotation.ToUnityQuaternion();
            }
        }

        void PostureChanged(PostureData data)
        {
            if (data.Point == PosturePoint.Bust)
            {
                this.Turret.rotation = data.Rotation.ToUnityQuaternion();
            }
        }
    }
}
