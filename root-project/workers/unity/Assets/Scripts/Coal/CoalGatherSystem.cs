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
    public class CoalGatherSystem : BaseEntitySearchSystem
    {
        private class DeleteCoalContext
        {
            public EntityId entityId;
        }

        EntityQuerySet querySet;
        EntityQueryBuilder.F_ECDDD<Transform, CoalStocks.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        const int frequency = 4; 
        readonly Collider[] colls = new Collider[256];

        const float gatherRange = 15.0f;

        int layer = int.MinValue;
        protected int CoalLayer
        {
            get
            {
                if (layer == int.MinValue)
                    layer = LayerMask.GetMask("Coal");

                return layer;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadWrite<CoalStocks.Component>(),
                                            ComponentType.ReadOnly<CoalStocks.HasAuthority>(),
                                            ComponentType.ReadOnly<Transform>(),
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<SpatialEntityId>()
                                        ), frequency);

            action = Query;
        }

        protected override void OnUpdate()
        {
            GatherCoals();
        }

        private void GatherCoals()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query (Unity.Entities.Entity entity,
                            Transform transform,
                            ref CoalStocks.Component stocks,
                            ref BaseUnitStatus.Component status,
                            ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            var pos = transform.position;

            int addCoal = 0;

            var count = Physics.OverlapSphereNonAlloc(pos, gatherRange, colls, this.CoalLayer);
            for (var i = 0; i < count; i++) {
                var col = colls[i];
                if (col.TryGetComponent<CoalInfoObject>(out var coal) == false)
                    continue;

                addCoal += coal.Gather();
            }

            if (addCoal > 0)
            {
                stocks.Stock += addCoal;
            }
        }
    }
}
