using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class HQInitializer : MonoBehaviour
    {
        [Require] CommandersManagerWriter writer;

        [SerializeField]
        HeadQuarterInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            writer.SendUpdate(new CommandersManager.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                SightRange = settings.SightRange,
                MaxRank = settings.MaxRank,
            });
        }
    }
}
