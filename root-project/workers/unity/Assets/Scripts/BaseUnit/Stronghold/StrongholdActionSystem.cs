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
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
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
                                          ref BaseUnitTarget.Component tgt,
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
                CheckAlive(trans.position, status.Side, ref stronghold);

                if (factory.TeamOrders.Count > 0)
                    return;
                
                var orders = makeOrders(stronghold.Rank, status.Order, stronghold.CommanderDatas);
                if (orders != null)
                    factory.TeamOrders.AddRange(orders);
            });
        }

        void CheckAlive(in Vector3 pos, UnitSide side,
                        ref StrongholdStatus.Component stronghold)
        {
            var datas = stronghold.CommanderDatas;
            var removeList = new List<EntityId>();
            foreach (var kvp in datas)
            {
                if (CheckAlive(kvp.Key.Id) == false)
                    removeList.Add(kvp.Key);
            }

            if (removeList.Count > 0) {
                foreach (var r in removeList)
                    datas.Remove(r);

                stronghold.CommanderDatas = datas;
            }
        }

        const int solnum = 5;
        const int underCommands = 3;
        List<TeamOrder> makeOrders(uint rank, OrderType order, Dictionary<EntityId,TeamInfo> datas)
        {
            uint maxrank = 0;

            TeamType teamType;
            switch (order)
            {
                case OrderType.Attack:
                    teamType = TeamType.Attacker;
                    maxrank = rank;
                    break;
                case OrderType.Guard:
                    teamType = TeamType.Guardian;
                    maxrank = rank;
                    break;   
                case OrderType.Keep:
                    teamType = TeamType.Guardian;
                    maxrank = 1;
                    break;
                case OrderType.Supply:
                    teamType = TeamType.Supplyer;
                    maxrank = 1;
                    break;
            }

            if (maxrank <= 0 || datas == null)
                return null;

            var teams = new List<TeamOrder>();
            int coms = 1;
            for(var r = maxrank; r >= 0; r--) {
                var count = datas.Count(kvp => kvp.Value.rank == r);
                if (count < coms) {
                    teams.Add(new TeamOrder()
                    {
                        CommanderRank = r,
                        SoldiersNumber = solnum,
                        Order = order,
                        Type = teamType,
                        Stack = coms - count,
                    });
                }

                coms *= underCommands;
            }

            return teams;
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
