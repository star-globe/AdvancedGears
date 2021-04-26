using System.Collections.Generic;
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
        private EntityQueryBuilder.F_EDDD<BaseUnitStatus.Component, StrongholdSight.Component, StrategyHexAccessPortal.Component> orderAction;
        private EntityQuerySet factoryQuerySet;
        private EntityQueryBuilder.F_EDDDD<UnitFactory.Component, BaseUnitStatus.Component, StrongholdSight.Component, StrategyHexAccessPortal.Component> factoryAction;

        readonly HashSet<EntityId> requestLists = new HashSet<EntityId>();
        readonly Dictionary<EntityId, TeamInfo> teamsDic = new Dictionary<EntityId, TeamInfo>();
        readonly HashSet<EntityId> teamKeys = new HashSet<EntityId>();
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
            
            orderAction = OrderQuery;
            factoryAction = FactoryQuery;
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

            Entities.With(orderQuerySet.group).ForEach(orderAction);
        }

        void OrderQuery(Unity.Entities.Entity entity,
                        ref BaseUnitStatus.Component status,
                        ref StrongholdSight.Component sight,
                        ref StrategyHexAccessPortal.Component portal)
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
        }

        void HandleFactoryRequests()
        {
            if (CheckTime(ref factoryQuerySet.inter) == false)
                return;

            Entities.With(factoryQuerySet.group).ForEach(factoryAction);
        }

        void FactoryQuery(Unity.Entities.Entity entity,
                        ref UnitFactory.Component factory,
                        ref BaseUnitStatus.Component status,
                        ref StrongholdSight.Component sight,
                        ref StrategyHexAccessPortal.Component portal)
        {
            if (status.State != UnitState.Alive)
                return;

            if (UnitUtils.IsBuilding(status.Type) == false)
                return;

            if (status.Side == UnitSide.None)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            CheckAlive(trans.position, status.Side, uint.MaxValue, HexDictionary.HexEdgeLength * 2, teamsDic);

            // number check
            if (factory.TeamOrders.Count == 0 && sight.StrategyVector.Side != UnitSide.None) {
                var teamOrders = factory.TeamOrders;
                makeOrders(status.Side, status.Rank, PostureUtils.RotFoward(sight.StrategyVector.Vector.ToUnityVector()), status.Order, portal.Index,
                            sight.FrontLineCorners, sight.TargetHexes, teamsDic, teamOrders);

                factory.TeamOrders = teamOrders;
            }

#if false
            if (factory.TurretOrders.Count == 0) {
                var turretOrders = factory.TurretOrders;
                makeOrders(trans.position, status.Side, status.Rank, turretOrders);
                factory.TurretOrders = turretOrders;
            }
#endif
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

            var units = getAllyUnits(side, pos, range, allowDead: false, GetSingleUnitTypes(UnitType.Commander));

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
            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.StrongholdRange), allowDead: false, GetSingleUnitTypes(UnitType.Turret));

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
        void makeOrders(UnitSide side, uint rank, float rot, OrderType order, uint hexIndex,
                                    List<FrontLineInfo> frontLines, Dictionary<uint, TargetHexInfo> hexes, Dictionary<EntityId,TeamInfo> datas, List<TeamOrder> teamOrders)
        {
            if (datas == null || teamOrders == null)
                return;

            if (hexes.Count > 0)
            {
                makeTeamOrders(hexes.Count * AttackLogicDictionary.TeamPerHex, strategyUnitRank, rot, order, teamOrders, datas);
            }
            else if (frontLines.Count > 0)
            {
                makeTeamOrders(frontLines.Count, strategyUnitRank, rot, order, teamOrders, datas);
            }
        }

        void makeTeamOrders(int coms, uint maxrank, float rot, OrderType order, List<TeamOrder> teamOrders, Dictionary<EntityId, TeamInfo> datas)
        {
            var solnum = AttackLogicDictionary.UnderSoldiers;
            var underCommands = AttackLogicDictionary.UnderCommanders;

            for (var r = maxrank; r >= 0; r--)
            {
                int count = 0;
                foreach (var kvp in datas) {
                    if (kvp.Value.Rank == r)
                        count++;
                }

                if (count < coms)
                {
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
        }


        void makeOrders(in Vector3 pos, UnitSide side, uint rank, List<TurretOrder> turretOrders)
        {
            if (turretOrders == null)
                return;

            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.StrongholdRange), allowDead: false, GetSingleUnitTypes(UnitType.Turret));
            var underTurrets = AttackLogicDictionary.UnderTurrets;

            int coms = 1;
            for(var r = rank; r >= 0; r--) {
                int count = 0;
                foreach (var u in units) {
                    if (u.rank == r)
                        count++;
                }

                if (count < coms) {
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

                var index = UnityEngine.Random.Range(0, count);
                int i = 0;
                UnitBaseInfo? baseInfo = null;
                foreach (var str in targetStrongholds) {
                    if (i == index)
                        baseInfo = str.Value;

                    i++;
                }

                if (baseInfo == null)
                    continue;

                //Debug.LogFormat("SelectStronghold Index:{0}, Key:{1}, Count:{2}, Target:{3}", hexIndex, kvp.Key, count, targetStrongholds[key].Position);

                var tgt = new TargetInfo() { TgtInfo = baseInfo.Value, };

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

                var frontLine = kvp.Value.TargetInfoSet.FrontLine;
                if (frontLine.IsValid() && lines.Contains(frontLine))
                    continue;

                var index = UnityEngine.Random.Range(0, lines.Count);

                var line = new TargetFrontLineInfo()
                {
                    FrontLine = lines[index],
                };

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
                TargetHexInfo? info = null;
                int i = 0;
                foreach (var tgt in targets) {
                    if (i == index)
                        info = tgt.Value;

                    i++;
                }

                if (info == null)
                    continue;

                SetCommand(kvp.Key, info.Value, order);

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
