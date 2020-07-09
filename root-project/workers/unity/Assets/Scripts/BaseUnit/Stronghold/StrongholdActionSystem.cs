using System.Collections.Generic;
using System.Linq;
using Improbable;
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
                                          ref UnitFactory.Component factory,
                                          ref BaseUnitStatus.Component status,
                                          ref StrongholdSight.Component sight,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, out var teams);
                CheckAliveTurret(trans.position, status.Side, out var turrets);

                // number check
                var id = entityId.EntityId;
                if (factory.TeamOrders.Count == 0) {
                    var teamOrders = makeOrders(status.Rank, status.Order, teams);
                    if (teamOrders != null)
                        factory.TeamOrders.AddRange(teamOrders);
                }

                if (factory.TurretOrders.Count == 0) {
                    var turretOrders = makeOrders(status.Rank, turrets);
                    if (turretOrders != null)
                        factory.TurretOrders.Add(turretOrders);
                }

                // order check
                // Strongholds
                CheckOrder(status.Order, sight.TargetStrongholds, teams);

                // FrontLineCorners
                CheckOrder(status.Order, sight.FrontLineCorners, teams);

                // Hex
                CheckOrder(status.Order, sight.TargetHexes, teams);
            });
        }

        void HandleResponses()
        {
            var responsesTeam = this.CommandSystem.GetResponses<UnitFactory.AddTeamOrder.ReceivedResponse>();
            for (var i = 0; i < responsesTeam.Count; i++) {
                ref readonly var response = ref responsesTeam[i];
                if (response.StatusCode != StatusCode.Success)
                    continue;

                requestLists.Remove(response.EntityId);
            }

            var responsesTurret = this.CommandSystem.GetResponses<UnitFactory.AddTurretOrder.ReceivedResponse>();
            for (var i = 0; i < responsesTurret.Count; i++)
            {
                ref readonly var response = ref responsesTurret[i];
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
                if (TryGetComponent<CommanderTeam.Component>(u.id, out var team) == false)
                    continue;

                var teamInfo = new TeamInfo()
                {
                    CommanderId = u.id,
                    Rank = u.rank,
                    Order = u.order,
                    TargetEntityId = team.Value.TargetStronghold.StrongholdId,
                };

                datas.Add(u.id, teamInfo);
            }
        }

        void CheckAliveTurret(in Vector3 pos, UnitSide side, out List<UnitInfo> turrets)
        {
            turrets = new List<UnitInfo>();
            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.StrongholdRange), allowDead: false, UnitType.Turret);

            foreach (var u in units) {
                if (TryGetComponent<TurretComponent.Component>(u.id, out var turret) == false)
                    continue;

                turrets.Add(u);
            }
        }

        void SendTeamOrders(EntityId id, List<TeamOrder> teamOrders)
        {
            this.CommandSystem.SendCommand(new UnitFactory.AddTeamOrder.Request(id, new TeamOrderList { Orders = teamOrders }));
            requestLists.Add(id);
        }

        void SendTurretOrders(EntityId id, List<TurretOrder> turretOrders)
        {
            this.CommandSystem.SendCommand(new UnitFactory.AddTurretOrder.Request(id, new TurretOrderList { Orders = turretOrders }));
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

        List<TurretOrder> makeOrders(uint rank, OrderType order, List<UnitInfo> currentTurrets)
        {
            var underTurrets = AttackLogicDictionary.DefenseTurrets;

            List<TurretOrder> turretOrders = null;
            int coms = 1;
            for(var r = rank; r >= 0; r--) {
                var count = currentTurrets.Count(kvp => kvp.Value.Rank == r);
                Debug.LogFormat("Turrets Count:{0} Rank:{1}", count, r);

                if (count < coms) {
                    turretOrders = turretOrders ?? new List<TurretOrder>();
                    turretOrders.Add(new TurretOrder()
                    {
                        TurretId = 1,
                        TurretsNumber = underTurrets,
                        Order = order,
                        Stack = coms - count,
                    });
                }

                coms *= underTurrets;

                if (r == 0)
                    break;
            }

            return turretOrders;
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

        private void SetCommand(EntityId id, in TargetHexInfo targetInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetTargetHex.Request(id, targetInfo));
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

        private void CheckOrder(OrderType order, Dictionary<EntityId,TargetHexInfo> targets, Dictionary<EntityId,TeamInfo> datas)
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

        void ProductAlly(in Vector3 pos, UnitSide side,
                        uint rank, in CommanderTeam.Component team,
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

            var n_sol = 0;
            if (n_sol > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = 0;
            if (n_com > 0 && rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = rank - 1 }));
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
