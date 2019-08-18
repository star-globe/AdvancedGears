using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class UnitFactoryInitializer : MonoBehaviour
    {
        [Require] UnitFactoryWriter factoryWriter;
        [Require] UnitArmyObserverWriter observerWriter;

        [SerializeField]
        UnitFactoryInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            factoryWriter.SendUpdate(new UnitFactory.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                ResourceMax = settings.ResourceMax,
            });

            observerWriter.SendUpdate(new UnitArmyObserver.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
            });
        }
    }
}
