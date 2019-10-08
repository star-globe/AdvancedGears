using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    class CommandersManagerSystem : BaseSearchSystem
    {
        private EntityQuery group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            group = GetEntityQuery(
                ComponentType.ReadWrite<CommandersManager.Component>(),
                ComponentType.ReadOnly<CommandersManager.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(CommandersManager.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref CommandersManager.Component manager,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.HeadQuarter)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (manager.State == CommanderManagerState.CreateCommander)
                    return;

                var time = Time.time;
                var inter = manager.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                manager.Interval = inter;

                if (manager.CommanderDatas.Count == 0) {
                    int rank = 0;

                    if (manager.factoryId.IsValid() == false) {
                        var trans = EntityManager.GetComponentObject<Transform>(entity);
                        var pos = trans.position;

                        var tgt = getNearestAlly(status.Side, pos, manager.SightRange, UnitType.Stronghold);
                        if (tgt != null)
                            manager.factoryId = tgt.id;
                    }

                    if (manager.factoryId.IsValid()) {
                        var id = entityId.EntityId;
                        var request = new UnitFactory.AddSuperiorOrder.Request(id, new SuperiorOrder() { Followers = new List<EntityId>(),
                                                                                                         HqEntityId = id,
                                                                                                         Side = side,
                                                                                                         Rank = rank + 1 });
                        Entity entity;
                        if (TryGetEntity(id, out entity)) {
                            this.CommandSystem.SendCommand(request, entity);
                            manager.State = CommanderManagerState.CreateCommander;
                        }
                    }
                }
            });
        }
    }
}
