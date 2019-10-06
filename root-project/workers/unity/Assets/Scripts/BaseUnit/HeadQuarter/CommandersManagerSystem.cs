using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    class CommandersManagerSystem : BaseSearchSystem
    {
        private EntityQuery group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            group = GetEntityQuery(
                ComponentType.ReadWrite<CommandersManager.Component>(),
                ComponentType.ReadOnly<CommandersManager.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()

            );

            group.SetFilter(CommandersManager.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref CommandersManager.Component manager,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {

            });
        }
    }
}
