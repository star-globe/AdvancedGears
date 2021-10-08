using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FlareUpdateSystem : BaseEntitySearchSystem
    {
        private class DeleteFlareContext
        {
            public EntityId entityId;
        }

        private EntityQuerySet querySet;
        private EntityQueryBuilder.F_EDD<StrategyFlare.Component, SpatialEntityId> action;

        private readonly HashSet<EntityId> deadUnitIds = new HashSet<EntityId>();
        private readonly HashSet<EntityId> deletedIds = new HashSet<EntityId>();

        const int period = 5;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<StrategyFlare.Component>(),
                                          ComponentType.ReadOnly<StrategyFlare.HasAuthority>(),
                                          ComponentType.ReadOnly<SpatialEntityId>()
                                          ), period);

            action = Query;
        }

        protected override void OnUpdate()
        {
            HandleDeadUnits();
            HandleCleanUnits();
            HandleDeleteResponses();
        }

        const float reviveTime = 3.0f;

        void HandleDeadUnits()
        {
            foreach (var id in deadUnitIds) {
                var comp = new BaseUnitReviveTimer.Component()
                {
                    IsStart = true,
                    RestTime = reviveTime,
                };
                base.SetComponent(id, comp);
            }

            deadUnitIds.Clear();
        }

        void HandleCleanUnits()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query(Entity entity,
                                ref StrategyFlare.Component flare,
                                ref SpatialEntityId entityId)
        {
            var time = Time.ElapsedTime - flare.LaunchTime;
            if (time < BulletDictionary.FlareLifeTime)
                return;

            var id = entityId.EntityId;
            if (deletedIds.Contains(id) == false) {
                var request = new WorldCommands.DeleteEntity.Request
                (
                    id,
                    context: new DeleteFlareContext() { entityId = id }
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
                if (!(response.Context is DeleteFlareContext requestContext)) {
                    // Ignore non-player entity creation requests
                    continue;
                }

                deletedIds.Remove(requestContext.entityId);
            }
        }

        public void AddDeadUnit(EntityId id)
        {
            if (deletedIds.Contains(id))
                return;

            deadUnitIds.Add(id);
        }
    }
}
