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
    internal class SleepUpdateSystem : SpatialComponentSystem
    {
        private EntityQuerySet querySet;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadOnly<SleepComponent.Component>(),
                                          ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                                        　ComponentType.ReadOnly<BaseUnitStatus.HasAuthority>()), 4);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref SleepComponent.Component sleep,
                                          ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var inter = sleep.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                status.State = UnitState.Sleep;
            });
        }
    }
}
