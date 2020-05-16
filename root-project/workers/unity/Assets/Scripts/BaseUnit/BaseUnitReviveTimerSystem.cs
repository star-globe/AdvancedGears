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
    public class BaseUnitReviveTimerSystem : BaseEntitySearchSystem
    {
        EntityQuery group;

        private class DeleteUnitContext
        {
            public EntityId entityId;
        }

        private readonly HashSet<EntityId> deadUnitIds = new HashSet<EntityId>();
        private readonly HashSet<EntityId> deletedIds = new HashSet<EntityId>();

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitReviveTimer.Component>(),
                ComponentType.ReadOnly<BaseUnitReviveTimer.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            HandleDeadUnits();
            HandleCleanUnits();
            HandleDeleteResponses();
        }

        const float time = 3.0f;

        void HandleDeadUnits()
        {
            foreach (var id in deadUnitIds) {
                var comp = new BaseUnitReviveTimer.Component()
                {
                    IsStart = true,
                    RestTime = time,
                };
                base.SetComponent(id, comp);
            }

            deadUnitIds.Clear();
        }


        void HandleCleanUnits()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitReviveTimer.Component revive,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (revive.IsStart == false)
                    return;

                if (status.Type.BaseType() == UnitBaseType.Fixed)
                    return;

                switch (status.State)
                {
                    case UnitState.None:
                        return;

                    case UnitState.Alive:
                        revive.IsStart = false;
                        revive.RestTime = 0.0f;
                        return;
                }

                if (revive.RestTime > 0)
                    revive.RestTime -= Time.DeltaTime;
                
                var id = entityId.EntityId;
                if (revive.RestTime < 0 && deletedIds.Contains(id) == false) {
                    var request = new WorldCommands.DeleteEntity.Request
                    (
                        id,
                        context: new DeleteUnitContext() { entityId = id }
                    );
                    this.CommandSystem.SendCommand(request);
                    deletedIds.Add(id);
                }
            });
        }

        void HandleDeleteResponses()
        {
            var responses = this.CommandSystem.GetResponses<WorldCommands.DeleteEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (!(response.Context is DeleteUnitContext requestContext)) {
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
