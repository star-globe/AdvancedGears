using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class UnitFactoryInitializer : MonoBehaviour
    {
        [Require] UnitFactoryWriter writer;

        [SerializeField]
        UnitFactoryInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            writer.SendUpdate(new UnitFactory.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                ResourceMax = settings.ResourceMax,
            });
        }
    }
}
