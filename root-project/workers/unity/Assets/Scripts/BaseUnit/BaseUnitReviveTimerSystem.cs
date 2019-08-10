using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class BaseUnitReviveTimerSystem : BaseEntitySearchSystem
    {
        EntityQuery group;

        private Vector3 origin;

        private class DeleteUnitContext
        {
            public EntityId entityId;
        }

        private readonly HashSet<long> deletedIds = new HashSet<long>();
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitReviveTimer.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(BaseUnitReviveTimer.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleCleanUnits();
            HandleDeleteResponses();
        }

        void HandleCleanUnits()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitReviveTimer.Component revive,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                switch (status.State)
                {
                    case UnitState.None:
                        return;

                    case UnitState.Alive:
                        base.RemoveComponent(entity, ComponentType.ReadOnly<BaseUnitReviveTimer.Component>());
                        return;
                }

                if (revive.RestTime > 0)
                    revive.RestTime -= Time.deltaTime;
                
                var id = entityId.EntityId;
                if (revive.RestTime < 0 && deletedIds.Contains(id.Id) == false)
                {
                    var request = new WorldCommands.DeleteEntity.Request
                    (
                        id,
                        context: new DeleteUnitContext() { entityId = id }
                    );
                    this.Command.SendCommand(request);
                    deletedIds.Add(id.Id);
                }
            });
        }

        void HandleDeleteResponses()
        {
            var responses = this.Command.GetResponses<WorldCommands.DeleteEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (!(response.Context is DeleteUnitContext requestContext)) {
                    // Ignore non-player entity creation requests
                    continue;
                }

                deletedIds.Remove(requestContext.entityId.Id);
            }
        }
    }
}
