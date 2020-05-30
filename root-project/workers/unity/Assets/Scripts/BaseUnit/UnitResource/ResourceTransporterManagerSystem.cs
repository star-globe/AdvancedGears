using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class ResourceTransporterManagerSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;
        const float time = 1.0f; 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<ResourceComponent.Component>(),
                ComponentType.ReadOnly<ResourceComponent.HasAuthority>(),
                ComponentType.ReadWrite<ResourceTransporter.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(time);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref ResourceComponent.Component resource,
                                          ref ResourceTransporter.Component transporter,
                                          ref BaseUnitTarget.Component target,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var range = transporter.Range;
                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var diff = target.TargetInfo.Position.ToWorkerPosition(this.Origin) - trans.position;
                if (diff.sqrMagnitude <= range * range) {
                    // todo:TransportAction
                }
            });
        }
    }
}
