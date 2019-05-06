using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using UnityEngine.Assertions;
using Improbable.Gdk.Core;

namespace Playground
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
            update.Interval = new IntervalChecker(inter,0);

            if (!data.Initialized)
            {
                update.Root = this.transform.rotation.ToImprobableQuaternion();

                var pos = new PostureData(PosturePoint.Bust, unit.Cannon.Turret.rotation.ToImprobableQuaternion());
                var dic = new Dictionary<PosturePoint, PostureData>();
                dic.Add(pos.Point, pos);
                update.Posture = new PostureInfo() { Datas = dic };

                update.Initialized = new Option<BlittableBool>(true);
            }

            writer.SendUpdate(update);
        }

    }
}
