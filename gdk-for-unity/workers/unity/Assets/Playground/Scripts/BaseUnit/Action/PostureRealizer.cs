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

            // initialize
            var data = reader.Data;
            this.transform.rotation = data.Root.ToUnityQuaternion();

            foreach(var k in unit.GetKeys())
            {
                PostureData pos;
                if (data.Posture.Datas.TryGetValue(k, out pos))
                {
                    int index = 0;
                    foreach (var r in pos.Rotations)
                    {
                        unit.SetQuaternion(k, index, r.ToUnityQuaternion());
                        index++;
                    }
                }
            }
        }

        void PostureChanged(PostureData data)
        {
            if (data.Point == PosturePoint.Bust)
            {
                this.Turret.rotation = data.Rotation.ToUnityQuaternion();
            }

            PostureTransform posture;
            if (unit.PostureDic.TryGetValue(data.Point, out posture) == false)
                return;

            int index = 0;
            foreach (var r in data.Rotations)
            {
                posture.SetQuaternion(index, r.ToUnityQuaternion());
                index++;
            }
        }
    }
}
