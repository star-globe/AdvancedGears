using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class WakerUpdateSystem : SpatialComponentSystem
    {
        private EntityQuerySet querySet;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<VirtualArmy.Component>(),
                                          ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                          ComponentType.ReadOnly<BaseUnitStatus.Component>()), 4);
        }

        protected override void OnUpdate()
        {
            
        }
    }
}
