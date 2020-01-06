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
    internal class StrongholdActionSystem : BaseSearchSystem
    {
        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrongholdStatus.Component>(),
                ComponentType.ReadOnly<StrongholdStatus.ComponentAuthority>(),
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<StrongholdSight.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(StrongholdStatus.ComponentAuthority.Authoritative);
            group.SetFilter(UnitFactory.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref StrongholdStatus.Component stronghold,
                                          ref UnitFactory.Component factory,
                                          ref BaseUnitStatus.Component status,
                                          ref StrongholdSight.Component sight,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var inter = stronghold.Interval;
                if (inter.CheckTime() == false)
                    return;

                stronghold.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, out var datas);

                // number check
                if (factory.TeamOrders.Count == 0) {
                    var teamOrders = makeOrders(stronghold.Rank, status.Order, datas);
                    if (teamOrders != null)
                        factory.TeamOrders.AddRange(orders);
                }

                // order check
                CheckOrder(status.Order, sight.TargetStronghold, datas);
            });
        }

        void CheckAlive(in Vector3 pos, UnitSide side, out Dictionary<EntityId,TeamInfo> datas)
        {
            datas = new Dictionary<EntityId, TeamInfo>();

            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.StrongholdRange), UnitType.Commander);

            foreach (var u in units) {
                if (TryGetComponent<CommanderStatus.Component>(u.id, out var comd) == false)
                    continue;

                if (TryGetComponent<CommanderTeam.Component>(u.id, out var team) == false)
                    continue;

                var teamInfo = new TeamInfo()
                {
                    CommanderId = u.id,
                    Rank = comd.Value.Rank,
                    Order = u.order,
                    TargetEntityId = team.Value.TargetStronghold.StrongholdId,
                };

                datas.Add(u.id, teamInfo);
            }
        }

        const int solnum = 5;
        const int underCommands = 3;
        List<TeamOrder> makeOrders(uint rank, OrderType order, Dictionary<EntityId,TeamInfo> datas, out List<EntityId> directOrders)
        {
            uint maxrank = 0;
            switch (order)
            {
                case OrderType.Attack:
                    maxrank = rank;
                    break;
                case OrderType.Guard:
                    maxrank = rank;
                    break;   
                case OrderType.Keep:
                    maxrank = 1;
                    break;
                case OrderType.Supply:
                    maxrank = 1;
                    break;
            }

            if (maxrank <= 0 || datas == null)
                return null;

            List<TeamOrder> teamOrders = null;
            int coms = 1;
            for(var r = maxrank; r >= 0; r--) {
                var count = datas.Count(kvp => kvp.Value.Rank == r);
                if (count < coms) {
                    teamOrders = teamOrders ?? new List<TeamOrder>();
                    teamOrders.Add(new TeamOrder()
                    {
                        CommanderRank = r,
                        SoldiersNumber = solnum,
                        Order = order,
                        Stack = coms - count,
                    });
                }

                coms *= underCommands;
            }

            return teamOrders;
        }

        private bool SetCommand(EntityId id, in TargetStrongholdInfo targetInfo, OrderType order)
        {
            if (base.SetCommand(id, order, out var entity) == false)
                return false;

            this.CommandSystem.SendCommand(new CommanderTeam.SetTargetStroghold.Request(id, targetInfo), entity);
            return true;
        }

        private void CheckOrder(OrderType order, in TargetStrongholdInfo target, Dictionary<EntityId,TeamInfo> datas)
        {
            List<EntityId> entityIds = null;
            foreach(var kvp in datas) {
                if (kvp.Value.Order != order ||
                    kvp.Value.TargetEntityId != target.StrongholdId) {
                    entityIds = entityIds ?? new List<EntityId>();
                    entityIds.Add(kvp.Key);
                }
            }

            if (entityIds != null) {
                foreach(var id in entityIds) {
                    SetCommand(id, target, order);
                }
            }
        }

        void ProductAlly(in Vector3 pos, UnitSide side,
                        in CommanderStatus.Component commander, in CommanderTeam.Component team,
                        in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetInfo.Position.ToWorkerPosition(this.Origin) - pos;
            float length = 10.0f;   // TODO from:master
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetInfo.TargetId;
            List<UnitFactory.AddFollowerOrder.Request> reqList = new List<UnitFactory.AddFollowerOrder.Request>();

            var n_sol = 0;// commander.TeamConfig.Soldiers - GetFollowerCount(team, false);
            if (n_sol > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = 0;// commander.TeamConfig.Commanders - GetFollowerCount(team,true);
            if (n_com > 0 && commander.Rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = commander.Rank - 1 }));
            }

            Entity entity;
            if (TryGetEntity(id, out entity) == false)
                return;

            foreach(var r in reqList)
                this.CommandSystem.SendCommand(r, entity);
            
            action.ActionType = CommandActionType.Product;
        }
    }
}
