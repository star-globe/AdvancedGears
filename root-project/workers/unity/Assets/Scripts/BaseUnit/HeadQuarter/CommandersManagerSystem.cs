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

                uint rank = 0;
                foreach (var kvp in manager.CommanderDatas)
                {
                    var r = kvp.Value.Rank;
                    if (r > rank)
                        rank = r;
                }

                if (rank < manager.MaxRank) {
                    if (manager.FactoryId.IsValid() == false) {
                        var trans = EntityManager.GetComponentObject<Transform>(entity);
                        var pos = trans.position;

                        var tgt = getNearestAlly(status.Side, pos, manager.SightRange, UnitType.Stronghold);
                        if (tgt != null)
                            manager.FactoryId = tgt.id;
                    }

                    if (manager.FactoryId.IsValid()) {
                        var id = entityId.EntityId;
                        var request = new UnitFactory.AddSuperiorOrder.Request(id, new SuperiorOrder() { Followers = new List<EntityId>(),
                                                                                                         HqEntityId = id,
                                                                                                         Side = status.Side,
                                                                                                         Rank = rank + 1 });
                        Entity factory;
                        if (TryGetEntity(id, out factory)) {
                            this.CommandSystem.SendCommand(request, factory);
                            manager.State = CommanderManagerState.CreateCommander;
                        }
                    }
                }
            });
        }
    }
}
