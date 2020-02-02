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
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class ResourceSupplyManagerSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;
        const float time = 1.0f; 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<ResourceComponent.Component>(),
                ComponentType.ReadOnly<ResourceComponent.ComponentAuthority>(),
                ComponentType.ReadWrite<ResourceSupplyer.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(ResourceComponent.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(time);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((ref ResourceComponent.Component resource,
                                          ref ResourceSupplyer.Component supplyer,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var current = Time.time;
                RecoveryResource(current, ref resource, ref supplyer);
            });
        }

        private void RecoveryResource(float current, ref ResourceComponent.Component resource, ref ResourceSupplyer.Component supplyer)
        {
            Debug.LogFormat("CurrentResource:{0}", resource.Resource);

            if (resource.Resource >= resource.ResourceMax) {
                resource.Resource = resource.ResourceMax;
                return;
            }

            if (supplyer.CheckedTime != 0.0f) {
                supplyer.ResourceFraction += (supplyer.CheckedTime - current) * supplyer.RecoveryRate;
                var add = Mathf.FloorToInt(supplyer.ResourceFraction);
                if (add > 0) {
                    var res = resource.Resource + add;
                    resource.Resource = Mathf.Min(res, resource.ResourceMax);
                    supplyer.ResourceFraction -= add;
                }
            }

            supplyer.CheckedTime = current;
        }
    }
}
