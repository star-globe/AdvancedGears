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
        [Require] ResourceComponentWriter resourceWriter;

        [SerializeField]
        UnitFactoryInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            factoryWriter.SendUpdate(new UnitFactory.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
            });

            resourceWriter.SendUpdate(new ResourceComponent.Update
            {
                ResourceMax = settings.ResourceMax,
                Resource = settings.ResourceMax,
                RecoveryRate = settings.RecoveryRate,
            });
        }
    }
}
