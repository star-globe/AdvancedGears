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

        readonly HashSet<EntityId> requestLists = new HashSet<EntityId>();
        readonly Dictionary<EntityId, TeamInfo> teamsDic = new Dictionary<EntityId, TeamInfo>();
        readonly List<UnitInfo> turretsList = new List<UnitInfo>();
        readonly HashSet<long> sendIds = new HashSet<long>();

        protected override void OnCreate()
        {
            base.OnCreate();

            orderQuerySet = new EntityQuerySet(GetEntityQuery(
                                               ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                               ComponentType.ReadOnly<StrongholdSight.Component>(),
                                               ComponentType.ReadOnly<StrategyHexAccessPortal.Component>(),
                                               ComponentType.ReadOnly<Transform>()
                                               ), 0.5f);

            factoryQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadWrite<UnitFactory.Component>(),
                                                ComponentType.ReadOnly<UnitFactory.HasAuthority>(),
                                                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                ComponentType.ReadOnly<StrongholdSight.Component>(),
                                                ComponentType.ReadOnly<StrategyHexAccessPortal.Component>(),
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
                                                        ref StrongholdSight.Component sight,
                                                        ref StrategyHexAccessPortal.Component portal) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, portal.Index, HexDictionary.HexEdgeLength, teamsDic);

                sendIds.Clear();

                // order check
                // Not Set Strongholds
                CheckOrder(portal.Index, status.Order, sight.TargetStrongholds, sight.StrategyVector.Vector.ToUnityVector(), teamsDic, sendIds);

                // FrontLineCorners
                CheckOrder(portal.Index, status.Order, status.Side, sight.FrontLineCorners, sight.StrategyVector.Vector.ToUnityVector(), teamsDic, sendIds);

                // Hex
                CheckOrder(portal.Index, status.Order, sight.TargetHexes, sight.StrategyVector.Vector.ToUnityVector(), teamsDic, sendIds);
            });
        }

        void HandleFactoryRequests()
        {
            if (CheckTime(ref factoryQuerySet.inter) == false)
                return;

            Entities.With(factoryQuerySet.group).ForEach((Unity.Entities.Entity entity,
                                                          ref UnitFactory.Component factory,
                                                          ref BaseUnitStatus.Component status,
                                                          ref StrongholdSight.Component sight,
                                                          ref StrategyHexAccessPortal.Component portal) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                CheckAlive(trans.position, status.Side, uint.MaxValue, HexDictionary.HexEdgeLength * 3, teamsDic);
                CheckAliveTurret(trans.position, status.Side, turretsList);

                // number check
                if (factory.TeamOrders.Count == 0) {
                    var teamOrders = makeOrders(status.Rank, PostureUtils.RotFoward(sight.StrategyVector.Vector.ToUnityVector()), status.Order, portal.Index,
                                                sight.FrontLineCorners, sight.TargetHexes, teamsDic);
                    if (teamOrders != null)
                    {
                        factory.TeamOrders.AddRange(teamOrders);
                        Debug.LogFormat("Add Orders Count:{0}", teamOrders.Count);
                    }
                }

                if (factory.TurretOrders.Count == 0) {
                    var turretOrders = makeOrders(status.Rank, turretsList);
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

        void CheckAlive(in Vector3 pos, UnitSide side, uint hexIndex, float range, Dictionary<EntityId,TeamInfo> datas)
        {
            if (datas == null)
                return;

            datas.Clear();

            var units = getAllyUnits(side, pos, range, allowDead: false, UnitType.Commander);

            foreach (var u in units) {
                if (hexIndex != uint.MaxValue && HexUtils.IsInsideHex(this.Origin, hexIndex, u.pos, HexDictionary.HexEdgeLength) == false)
                    continue;

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

        void CheckAliveTurret(in Vector3 pos, UnitSide side, List<UnitInfo> turrets)
        {
            if (turrets == null)
                return;

            turrets.Clear();
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

        const int strategyUnitRank = 0;
        const int hexAttackRate = 3;
        List<TeamOrder> makeOrders(uint rank, float rot, OrderType order, uint hexIndex, List<FrontLineInfo> frontLines, Dictionary<uint, TargetHexInfo> hexes, Dictionary<EntityId,TeamInfo> datas)
        {
            if (datas == null)
                return null;

            List<TeamOrder> teamOrders = null;

            if (hexes.Count > 0)
            {
                teamOrders = makeTeamOrders(hexes.Count * hexAttackRate, strategyUnitRank, rot, order, teamOrders, datas);
            }
            else if (frontLines.Count > 0)
            {
                teamOrders = makeTeamOrders(frontLines.Count, strategyUnitRank, rot, order, teamOrders, datas);
            }

            return teamOrders;
        }

        List<TeamOrder> makeTeamOrders(int coms, uint maxrank, float rot, OrderType order, List<TeamOrder> teamOrders, Dictionary<EntityId, TeamInfo> datas)
        {
            var solnum = AttackLogicDictionary.UnderSoldiers;
            var underCommands = AttackLogicDictionary.UnderCommanders;

            for (var r = maxrank; r >= 0; r--)
            {
                var count = datas.Count(kvp => kvp.Value.Rank == r);
                if (count < coms)
                {
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


        private void SetCommand(EntityId id, in TargetInfo targetInfo, OrderType order)
        {
            base.SetOrder(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetTarget.Request(id, targetInfo));
        }

        private void SetCommand(EntityId id, in TargetFrontLineInfo targetInfo, OrderType order)
        {
            base.SetOrder(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetFrontline.Request(id, targetInfo));
        }

        private void SetCommand(EntityId id, in TargetHexInfo targetInfo, OrderType order)
        {
            base.SetOrder(id, order);
            this.CommandSystem.SendCommand(new CommanderTeam.SetHex.Request(id, targetInfo));
        }

        private void CheckOrder(uint hexIndex, OrderType order, Dictionary<EntityId,UnitBaseInfo> targetStrongholds, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas, HashSet<long> sendIds)
        {
            if (sendIds == null)
                return;

            var count = targetStrongholds.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                var uid = kvp.Key.Id;

                if (sendIds.Contains(uid))
                    continue;

                var stronghold = kvp.Value.TargetInfoSet.Stronghold.UnitId;
                if (stronghold.IsValid() && targetStrongholds.ContainsKey(stronghold))
                    continue;

                var key = targetStrongholds.Keys.ElementAt(UnityEngine.Random.Range(0,count));

                Debug.LogFormat("SelectStronghold Index:{0}, Key:{1}, Count:{2}, Target:{3}", hexIndex, kvp.Key, count, targetStrongholds[key].Position);

                var tgt = new TargetInfo()
                {
                    TgtInfo = targetStrongholds[key],
                };

                SetCommand(kvp.Key, tgt, order);

                sendIds.Add(kvp.Key.Id);
            }
        }

        private void CheckOrder(uint hexIndex, OrderType order, UnitSide side, List<FrontLineInfo> lines, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas, HashSet<long> sendIds)
        {
            if (sendIds == null)
                return;

            var count = lines.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                var uid = kvp.Key.Id;
                if (sendIds.Contains(uid))
                    continue;

                //if (kvp.Value.Rank > 0)
                //    continue;

                var frontLine = kvp.Value.TargetInfoSet.FrontLine;
                if (frontLine.IsValid() && lines.Contains(frontLine))
                    continue;

                var index = UnityEngine.Random.Range(0, lines.Count);

                var line = new TargetFrontLineInfo()
                {
                    FrontLine = lines[index],
                };

                Debug.LogFormat("SelectLine Index:{0}, Key:{1}, Count:{2} Target_Left:{3} Target_Left:{4}", hexIndex, kvp.Key, count, lines[index].LeftCorner, lines[index].RightCorner);

                SetCommand(kvp.Key, line, order);

                sendIds.Add(uid);
            }
        }

        private void CheckOrder(uint hexIndex, OrderType order, Dictionary<uint,TargetHexInfo> targets, Vector3 strategyVector, Dictionary<EntityId,TeamInfo> datas, HashSet<long> sendIds)
        {
            if (sendIds == null)
                return;

            var count = targets.Count;
            if (count == 0)
                return;

            foreach(var kvp in datas) {
                var uid = kvp.Key.Id;
                if (sendIds.Contains(uid))
                    continue;

                var hexInfo = kvp.Value.TargetInfoSet.HexInfo;
                if (hexInfo.IsValid() && targets.ContainsKey(hexInfo.HexIndex))
                    continue;

                var index = UnityEngine.Random.Range(0, count);
                var key = targets.Keys.ElementAt(index);

                Debug.LogFormat("SelectHex Index:{0}, Key:{1}, Count:{2} Target:{3}", hexIndex, kvp.Key.Id, count, targets[key].HexInfo.HexIndex);

                SetCommand(kvp.Key, targets[key], order);

                sendIds.Add(uid);
            }
        }

        void ProductAlly(in Vector3 pos, UnitSide side,
                        uint rank, in CommanderTeam.Component team,
                        in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetUnit.Position.ToWorkerPosition(this.Origin) - pos;
            float length = 10.0f;   // TODO from:master
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetUnit.UnitId;
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
