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
        private EntityQuerySet orderQuerySet;
        private EntityQuerySet factoryQuerySet;

        IntervalChecker inter;

        readonly HashSet<EntityId> requestLists = new HashSet<EntityId>();

        protected override void OnCreate()
        {
            base.OnCreate();

            orderQuerySet = new EntityQuerySet(GetEntityQuery(
                                               ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                               ComponentType.ReadOnly<StrongholdSight.Component>(),
                                               ComponentType.ReadOnly<Transform>()
                                               ), 0.5f);

            factoryQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadWrite<UnitFactory.Component>(),
                                                ComponentType.ReadOnly<UnitFactory.HasAuthority>(),
                                                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                ComponentType.ReadOnly<StrongholdSight.Component>(),
                                                ComponentType.ReadOnly<Transform>()
                                                ), 2);
        }

        protected override void OnUpdate()
        {
            HandleOrders();
            HandleFactoryRequests();
            HandleResponses();
        }

        void HandleOrders()
        {
            if (CheckTime(ref orderQuerySet.inter) == false)
                return;

            Entities.With(orderQuerySet.group).ForEach((Unity.Entities.Entity entity,
                                                        ref BaseUnitStatus.Component status,
                                                        ref StrongholdSight.Component sight) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, out var teams);

                // order check
                // Not Set Strongholds
                CheckOrder(status.Order, sight.TargetStrongholds, sight.StrategyVector.Vector.ToUnityVector(), teams);

                // FrontLineCorners
                CheckOrder(status.Order, status.Side, sight.FrontLineCorners, sight.StrategyVector.Vector.ToUnityVector(), teams);

                // Hex
                CheckOrder(status.Order, sight.TargetHexes, sight.StrategyVector.Vector.ToUnityVector(), teams);
            });
        }

        void HandleFactoryRequests()
        {
            if (CheckTime(ref factoryQuerySet.inter) == false)
                return;

            Entities.With(factoryQuerySet.group).ForEach((Unity.Entities.Entity entity,
                                                          ref UnitFactory.Component factory,
                                                          ref BaseUnitStatus.Component status,
                                                          ref StrongholdSight.Component sight) =>
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
                if (factory.TeamOrders.Count == 0) {
                    var teamOrders = makeOrders(status.Rank, PostureUtils.RotFoward(sight.StrategyVector.Vector.ToUnityVector()), status.Order, teams);
                    if (teamOrders != null)
                        factory.TeamOrders.AddRange(teamOrders);
                }

                if (factory.TurretOrders.Count == 0) {
                    var turretOrders = makeOrders(status.Rank, turrets);
                    if (turretOrders != null)
                        factory.TurretOrders.AddRange(turretOrders);
                }
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
                    TargetInfoSet = team.Value.TargetInfoSet,
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

        List<TeamOrder> makeOrders(uint rank, float rot, OrderType order, Dictionary<EntityId,TeamInfo> datas)
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
                        Rot = rot,
                    });
                }

                coms *= underCommands;

                if (r == 0)
                    break;
            }

            return teamOrders;
        }

        List<TurretOrder> makeOrders(uint rank, List<UnitInfo> currentTurrets)
        {
            var underTurrets = AttackLogicDictionary.UnderTurrets;

            List<TurretOrder> turretOrders = null;
            int coms = 1;
            for(var r = rank; r >= 0; r--) {
                var count = currentTurrets.Count(u => u.rank == r);
                Debug.LogFormat("Turrets Count:{0} Rank:{1}", count, r);

                if (count < coms) {
                    turretOrders = turretOrders ?? new List<TurretOrder>();
                    turretOrders.Add(new TurretOrder()
                    {
                        TurretId = 1,
                        TurretsNumber = underTurrets,
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

        private void CheckOrder(OrderType order, Dictionary<EntityId,TargetStrongholdInfo> targetStrongholds, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas)
        {
            var count = targetStrongholds.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                var stronghold = kvp.Value.TargetInfoSet.Stronghold.StrongholdId;
                if (stronghold.IsValid() && targetStrongholds.ContainsKey(stronghold))
                    continue;

                var key = targetStrongholds.Keys.ElementAt(UnityEngine.Random.Range(0,count));
                SetCommand(kvp.Key, targetStrongholds[key], order);
            }
        }

        private void CheckOrder(OrderType order, UnitSide side, List<FrontLineInfo> lines, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas)
        {
            var count = lines.Count;
            if (count == 0)
                return;

            DebugUtils.RandomlyLog(string.Format("Side:{0} LinesCount:{1}", side, lines.Count));

            foreach(var kvp in datas) {
                var frontLine = kvp.Value.TargetInfoSet.FrontLine.FrontLine;
                if (frontLine.IsValid() && lines.Contains(frontLine))
                    continue;

                var index = UnityEngine.Random.Range(0, lines.Count);

                var line = new TargetFrontLineInfo()
                {
                    FrontLine = lines[index],
                };

                SetCommand(kvp.Key, line, order);
            }
        }

        private void CheckOrder(OrderType order, Dictionary<uint,TargetHexInfo> targets, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas)
        {
            var count = targets.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                var hexInfo = kvp.Value.TargetInfoSet.HexInfo;
                if (hexInfo.IsValid() && targets.ContainsKey(hexInfo.HexIndex))
                    continue;

                var index = UnityEngine.Random.Range(0, count);
                var key = targets.Keys.ElementAt(index);

                Debug.LogFormat("SelectHEx Index:{0}, Key:{1}, Count:{2}", index, key, count);

                SetCommand(kvp.Key, targets[key], order);
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
