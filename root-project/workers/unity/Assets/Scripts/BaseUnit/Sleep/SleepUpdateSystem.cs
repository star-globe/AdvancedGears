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
        private EntityQueryBuilder.F_EDD<SleepComponent.Component, BaseUnitStatus.Component> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadOnly<SleepComponent.Component>(),
                                          ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                                        　ComponentType.ReadOnly<BaseUnitStatus.HasAuthority>()), 4);
            action = Query;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query(Entity entity,
                                ref SleepComponent.Component sleep,
                                ref BaseUnitStatus.Component status)
        {
                if (status.State != UnitState.Alive)
                    return;

                var inter = sleep.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                status.State = UnitState.Sleep;
        }
    }
}
