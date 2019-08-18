using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class HQInitializer : MonoBehaviour
    {
        [Require] HeadQuartersWriter writer;

        [SerializeField]
        HeadQuarterInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            writer.SendUpdate(new HeadQuarters.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                MaxRank = settings.MaxRank,
            });
        }
    }
}
