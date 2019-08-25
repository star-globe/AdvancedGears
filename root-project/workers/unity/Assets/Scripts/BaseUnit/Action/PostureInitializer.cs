using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;

namespace AdvancedGears
{
    public class PostureInitializer : MonoBehaviour
    {
        [Require] BaseUnitPostureWriter writer;

        [SerializeField] UnitTransform unit;
        [SerializeField] float inter = 0.6f;

        private void Start()
        {
            Assert.IsNotNull(unit);
        }

        private void OnEnable()
        {
            var data = writer.Data;

            var update = new BaseUnitPosture.Update();
            update.Interval = IntervalCheckerInitializer.InitializedChecker(inter);

            if (!data.Initialized)
            {
                update.Root = this.transform.rotation.ToCompressedQuaternion();

                var dic = new Dictionary<PosturePoint, PostureData>();
                foreach (var k in unit.GetKeys())
                {
                    var pos = new PostureData(k, unit.GetAllRotates(k));
                    dic.Add(pos.Point, pos);
                }

                update.Posture = new PostureInfo() { Datas = dic };

                update.Initialized = true;
            }

            writer.SendUpdate(update);
        }

    }
}
