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
    public class StrategySearchHexSystem : SpatialComponentSystem
    {
        EntityQuery managerGroup;
        EntityQuery hexGroup;
        IntervalChecker interManager;
        IntervalChecker interHex;
        const int frequencyManager = 5; 
        const int frequencyHex = 15; 

        protected override void OnCreate()
        {
            base.OnCreate();

            managerGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexManager.Component>(),
                ComponentType.ReadOnly<StrategyHexManager.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>()
            );

            hexGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interManager = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyManager);
            interHex = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyHex);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref interManager)) {
                Entities.With(managerGroup).ForEach((Entity entity,
                                          ref StrategyHexManager.Component strategy,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
                {

                });
            }

            if (CheckTime(ref interHex)) {
                Entities.With(hexGroup).ForEach((Entity entity,
                                          ref HexBase.Component hex,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
                {

                });
            }
        }
    }
}
