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

        private readonly HashSet<EntityId> removeCoalIds = new HashSet<EntityId>();
        private readonly HashSet<EntityId> deletedIds = new HashSet<EntityId>();

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadWrite<CoalStocks.Component>(),
                                            ComponentType.ReadOnly<CoalStocks.HasAuthority>(),
                                            ComponentType.ReadOnly<Transform>(),
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<SpatialEntityId>()
                                        ), frequency;

            action = Query;
        }

        protected override void OnUpdate()
        {
            GatherCoals();
            RemoveCoals();
            HandleDeleteResponses();
        }

        private void GatherCoals()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            removeCoalIds.Clear();
            Entities.With(querySet.group).ForEach(action);
        }

        private void RemoveCoals()
        {
            foreach (var id in removeCoalIds) {
                var request = new WorldCommands.DeleteEntity.Request
                (
                    id,
                    context: new DeleteCoalContext() { entityId = id }
                );
                this.CommandSystem.SendCommand(request);
                deletedIds.Add(id);
            }
        }

        void HandleDeleteResponses()
        {
            var responses = this.CommandSystem.GetResponses<WorldCommands.DeleteEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (!(response.Context is DeleteCoalContext requestContext)) {
                    // Ignore non-player entity creation requests
                    continue;
                }

                deletedIds.Remove(requestContext.entityId);
            }
        }

        private void Query (Entity entity,
                            Transform transform,
                            ref CoalStocks.Component stocks,
                            ref BaseUnitStatus.Component status,
                            ref SpatialEntityId entityId) =>
        {
            if (status.State != UnitState.Alive)
                return;

            var pos = transform.position;

            int addCoal = 0;

            var count = Physics.OverlapSphereNonAlloc(pos, gatherRange, colls, this.CoalLayer);
            for (var i = 0; i < count; i++) {
                var col = colls[i];
                if (col.TryGetComponent<LinkedEntityComponent>(out var comp) == false)
                    continue;

                var entityId = comp.EntityId;
                if (removeCoalIds.Contains(entityId) || deletedIds.Contains(entityId))
                    continue;

                CoalSolids.Component? coal;
                if (TryGetComponent(entityId, out coal) == false)
                    continue;

                addCoal += coal.Value.Amount;
                removeCoalIds.Add(entityId);
            }

            if (addCoal > 0)
            {
                stocks.Stock += addCoal;
            }
        }
    }
}
