using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class HealthRecoveryInHexSystem : HexUpdateBaseSystem
    {
        EntityQuery group;
        IntervalChecker inter;
        const int frequency = 1; 

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitHealth.Component>(),
                ComponentType.ReadOnly<BaseUnitHealth.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f / frequency);
        }
    }
}
