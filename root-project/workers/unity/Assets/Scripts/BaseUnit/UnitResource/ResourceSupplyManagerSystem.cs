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
    [Obsolete]
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
                ComponentType.ReadOnly<ResourceComponent.HasAuthority>(),
                ComponentType.ReadWrite<ResourceSupplyer.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(time);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
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

                var current = Time.ElapsedTime;
                RecoveryResource(current, ref resource, ref supplyer);
            });
        }

        private void RecoveryResource(double current, ref ResourceComponent.Component resource, ref ResourceSupplyer.Component supplyer)
        {
            //Debug.LogFormat("CurrentResource:{0}", resource.Resource);

            if (resource.Resource >= resource.ResourceMax) {
                resource.Resource = resource.ResourceMax;
                return;
            }

            if (supplyer.CheckedTime != 0.0f) {
                supplyer.ResourceFraction += (float)(supplyer.CheckedTime - current) * supplyer.RecoveryRate;
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
