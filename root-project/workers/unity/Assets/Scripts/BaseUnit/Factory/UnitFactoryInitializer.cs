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
        [Require] ResourceSupplyerWriter supplyerWriter;

        [SerializeField]
        UnitFactoryInitSettings settings;

        void Start()
        {
            Assert.IsNotNull(settings);

            resourceWriter.SendUpdate(new ResourceComponent.Update
            {
                ResourceMax = settings.ResourceMax,
                Resource = settings.ResourceMax,
            });

            supplyerWriter.SendUpdate(new ResourceSupplyer.Update
            {
                RecoveryRate = settings.RecoveryRate,
            });
        }
    }
}
