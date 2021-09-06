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
    public class PlayerReviveTimerSystem : BaseEntitySearchSystem
    {
        private EntityQuerySet querySet;
        private EntityQueryBuilder.F_EDDD<PlayerRespawn.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        private readonly HashSet<EntityId> deadUnitIds = new HashSet<EntityId>();
        private readonly HashSet<EntityId> deletedIds = new HashSet<EntityId>();

        double deltaTime = -1;

        const int period = 5;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<PlayerRespawn.Component>(),
                                          ComponentType.ReadOnly<PlayerRespawn.HasAuthority>(),
                                          ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                          ComponentType.ReadOnly<SpatialEntityId>()
                                          ), period);

            deltaTime = Time.ElapsedTime;

            action = Query;
        }

        protected override void OnUpdate()
        {
            HandleDeadUnits();
            HandleCleanUnits();
        }

        const float reviveTime = 3.0f;

        void HandleDeadUnits()
        {
            foreach (var id in deadUnitIds) {
                var comp = new PlayerRespawn.Component()
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

            deltaTime = Time.ElapsedTime - deltaTime;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query(Entity entity,
                                ref PlayerRespawn.Component revive,
                                ref BaseUnitStatus.Component status,
                                ref SpatialEntityId entityId)
        {
            if (revive.IsStart == false)
                return;

            if (status.Type != UnitType.Advanced)
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
                revive.RestTime -= deltaTime;
                
            var id = entityId.EntityId;
            if (revive.RestTime < 0 && deletedIds.Contains(id) == false) {
                this.UpdateSystem.SendEvent(new PlayerRespawn.Respawn.Event(new Empty()), id);
                deletedIds.Add(id);
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
