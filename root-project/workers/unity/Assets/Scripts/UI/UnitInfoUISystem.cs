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
using AdvancedGears;

namespace AdvancedGears.UI
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class UnitUIInfoSystem : BaseSearchSystem
    {
        private EntityQuery group;

        public UnitUICreator UnitUICreator { private get; set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitHealth.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitHealth.Component health,
                                          ref SpatialEntityId entityId) =>
            {
            });
        }
    }
}
