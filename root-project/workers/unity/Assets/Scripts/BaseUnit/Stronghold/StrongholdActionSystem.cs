using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Worker.CInterop;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class StrongholdActionSystem : BaseSearchSystem
    {
        private EntityQuery group;

        IntervalChecker inter;

        readonly HashSet<EntityId> requestLists = new HashSet<EntityId>();

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<StrongholdStatus.Component>(),
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<StrongholdSight.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(0.5f);
        }

        protected override void OnUpdate()
        {
            HandleRequests();
            HandleResponses();
        }

        void HandleRequests()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Unity.Entities.Entity entity,
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

                if (status.Side == UnitSide.None)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, out var datas);

                // number check
                var id = entityId.EntityId;
                if (factory.TeamOrders.Count == 0) {
                    var teamOrders = makeOrders(stronghold.Rank, status.Order, datas);
                    if (teamOrders != null)
                        factory.TeamOrders.AddRange(teamOrders);
                }

                // order check
                CheckOrder(status.Order, sight.TargetStrongholds, datas);
            });
        }

        void HandleResponses()
        {
            var responses = this.CommandSystem.GetResponses<UnitFactory.AddTeamOrder.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (response.StatusCode != StatusCode.Success)
                    continue;

                requestLists.Remove(response.EntityId);
            }
        }

        void CheckAlive(in Vector3 pos, UnitSide side, out Dictionary<EntityId,TeamInfo> datas)
        {
            datas = new Dictionary<EntityId, TeamInfo>();

            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.StrongholdRange), allowDead: false, UnitType.Commander);

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

        void SendTeamOrders(EntityId id, List<TeamOrder> teamOrders)
        {
            this.CommandSystem.SendCommand(new UnitFactory.AddTeamOrder.Request(id, new TeamOrderList { Orders = teamOrders }));
            requestLists.Add(id);
        }

        List<TeamOrder> makeOrders(uint rank, OrderType order, Dictionary<EntityId,TeamInfo> datas)
        {
            var maxrank = OrderDictionary.GetMaxRank(order, rank);

            if (maxrank <= 0 || datas == null)
                return null;

            var solnum = AttackLogicDictionary.UnderSoldiers;
            var underCommands = AttackLogicDictionary.UnderCommanders;

            List<TeamOrder> teamOrders = null;
            int coms = 1;
            for(var r = maxrank; r >= 0; r--) {
                var count = datas.Count(kvp => kvp.Value.Rank == r);

                Debug.LogFormat("Commanders Count:{0} Rank:{1}", count, r);

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

                if (r == 0)
                    break;
            }

            return teamOrders;
        }

        private void SetCommand(EntityId id, in TargetStrongholdInfo targetInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetTargetStroghold.Request(id, targetInfo));
        }

        private void SetCommand(EntityId id, in TargetFrontLineInfo targetInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetTargetFrontline.Request(id, targetInfo));
        }

        readonly Dictionary<EntityId, List<EntityId>> entityIds = new Dictionary<EntityId, List<EntityId>>();
        private void CheckOrder(OrderType order, Dictionary<EntityId,TargetStrongholdInfo> targets, Dictionary<EntityId,TeamInfo> datas)
        {
            entityIds.Clear();

            var count = targets.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                 if(targets.ContainsKey(kvp.Value.TargetEntityId) == false) {
                    var key = targets.Keys.ElementAt(UnityEngine.Random.Range(0,count));

                    if (entityIds.ContainsKey(key) == false)
                        entityIds[key] = new List<EntityId>();

                    entityIds[key].Add(kvp.Key);
                 }
            }

            foreach (var kvp in entityIds)
            {
                var target = targets[kvp.Key];
                foreach (var id in kvp.Value) {
                    SetCommand(id, target, order);
                }
            }
        }

        private void CheckOrder(OrderType order, List<Coordinates> corners, Dictionary<EntityId,TeamInfo> datas)
        {
            entityIds.Clear();

            var count = corners.Count;
            if (count <= 1)
                return;

            int index = 0;
            foreach(var kvp in datas) {
                if (index + 1 >= corners.Count)
                    index = 0;

                var line = new TargetFrontLineInfo()
                {
                    LeftCorner = corners[index],
                    RightCorner = corners[index + 1]
                };

                SetCommand(kvp.Key, line, order);
                index++;
                // if(targets.ContainsKey(kvp.Value.TargetEntityId) == false) {
                //    var key = targets.Keys.ElementAt(UnityEngine.Random.Range(0,count));
                //
                //    if (entityIds.ContainsKey(key) == false)
                //        entityIds[key] = new List<EntityId>();
                //
                //    entityIds[key].Add(kvp.Key);
                // }
            }

            //foreach (var kvp in entityIds)
            //{
            //    var target = targets[kvp.Key];
            //    foreach (var id in kvp.Value) {
            //        SetCommand(id, target, order);
            //    }
            //}
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
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = 0;// commander.TeamConfig.Commanders - GetFollowerCount(team,true);
            if (n_com > 0 && commander.Rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = commander.Rank - 1 }));
            }

            Unity.Entities.Entity entity;
            if (TryGetEntity(id, out entity) == false)
                return;

            foreach(var r in reqList)
                this.CommandSystem.SendCommand(r, entity);
            
            action.ActionType = CommandActionType.Product;
        }
    }
}
