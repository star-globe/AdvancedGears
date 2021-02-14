using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class CommanderUnitSearchSystem : BaseCommanderSearch
    {
        private EntityQuerySet targettingQuerySet;
        private EntityQuerySet teamingQuerySet;

        //private EntityQuery targettingGroup;
        //private EntityQuery teamingGroup;
        //
        //IntervalChecker teamingInter;
        //IntervalChecker targettingInter;

        const float teaminTime = 1.0f;
        const int period = 15;
        #region ComponentSystem
        protected override void OnCreate()
        {
            base.OnCreate();

            targettingQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<CommanderStatus.Component>(),
                                                    ComponentType.ReadOnly<CommanderStatus.HasAuthority>(),
                                                    ComponentType.ReadWrite<CommanderTeam.Component>(),
                                                    ComponentType.ReadOnly<CommanderTeam.HasAuthority>(),
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                    ComponentType.ReadOnly<BaseUnitAction.Component>(),
                                                    ComponentType.ReadOnly<Transform>(),
                                                    ComponentType.ReadOnly<SpatialEntityId>()
                                                    ), period);

            teamingQuerySet = new EntityQuerySet(GetEntityQuery(
                                                 ComponentType.ReadWrite<CommanderTeam.Component>(),
                                                 ComponentType.ReadOnly<CommanderTeam.HasAuthority>(),
                                                 ComponentType.ReadOnly<CommanderSight.Component>(),
                                                 ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                 ComponentType.ReadOnly<Transform>(),
                                                 ComponentType.ReadOnly<SpatialEntityId>()
                                                ), teaminTime);

            //teamingGroup = GetEntityQuery(
            //    ComponentType.ReadWrite<CommanderTeam.Component>(),
            //    ComponentType.ReadOnly<CommanderTeam.HasAuthority>(),
            //    ComponentType.ReadOnly<CommanderSight.Component>(),
            //    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
            //    ComponentType.ReadOnly<Transform>(),
            //    ComponentType.ReadOnly<SpatialEntityId>()
            //);
            //
            //teamingInter = IntervalCheckerInitializer.InitializedChecker(teaminTime);
            //targettingInter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            HandleTeaming();
            HandleTargetting();
        }
        private void HandleTargetting()
        {
            if (CheckTime(ref targettingQuerySet.inter) == false)
                return;

            Entities.With(targettingQuerySet.group).ForEach((Entity entity,
                              ref CommanderStatus.Component commander,
                              ref CommanderTeam.Component team,
                              ref BaseUnitStatus.Component status,
                              ref BaseUnitAction.Component action,
                              ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Commander)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                applyOrder(status, entityId, pos, action.SightRange, ref commander, ref team);
            });
        }
        
        private void HandleTeaming()
        {
            if (CheckTime(ref teamingQuerySet.inter) == false)
                return;

            Entities.With(teamingQuerySet.group).ForEach((Entity entity,
                              ref CommanderSight.Component sight,
                              ref CommanderTeam.Component team,
                              ref BaseUnitStatus.Component status,
                              ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsOfficer(status.Type) == false)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (status.Rank == 0)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                team.FollowerInfo.UnderCommanders.Clear();

                var rank = status.Rank - 1;
                var range = RangeDictionary.GetBoidsRange(status.Rank);
                var allies = getAllyUnits(status.Side, pos, range, allowDead:false, UnitType.Commander);
                foreach(var unit in allies) {
                    if (unit.id == entityId.EntityId)
                        continue;

                    BaseUnitStatus.Component? sts;
                    if (this.TryGetComponent(unit.id, out sts) == false)
                        continue;
                    
                    if (sts.Value.Rank != rank)
                        continue;

                    team.FollowerInfo.UnderCommanders.Add(unit.id);
                }
            });
        }
        #endregion

        void commonTargeting(UnitInfo tgt, in SpatialEntityId entityId, in CommanderStatus.Component commander, out TargetInfo targetInfo)
        {
            UnitBaseInfo baseInfo; 
            baseInfo.UnitId = tgt.id;
            baseInfo.Position = tgt.pos.ToWorldCoordinates(this.Origin);
            baseInfo.Type = tgt.type;
            baseInfo.Side = tgt.side;
            baseInfo.State = tgt.state;

            targetInfo = new TargetInfo(baseInfo,1.0f);
        }

        #region OrderMethod

        void applyOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, float sightRange,
                         ref CommanderStatus.Component commander,
                         ref CommanderTeam.Component team)
        {
            // check rank
            UnitInfo tgt;

            var scaledRange = AttackLogicDictionary.RankScaled(sightRange, status.Rank);

            // check power
            var current = GetOrder(status.Side, pos, scaledRange, out float rate);
            if (current == null)
            {
                current = commander.Order.Upper == OrderType.Guard ?
                            OrderType.Guard : OrderType.Keep;
            }

            var changed = status.Order != current.Value;

            var line = team.TargetInfoSet.FrontLine;
            var hex = team.TargetInfoSet.HexInfo;
            var stronghold = team.TargetInfoSet.Stronghold;
            var followers = team.FollowerInfo.GetAllFollowers();

            var targetBit = team.IsNeedUpdate;

            bool isSetRate = false;

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.FrontLine) && line.IsValid()) {
                SetOrderFollowers(followers, entityId.EntityId, line, current.Value, rate);
                isSetRate = true;
            }

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.Hex) && hex.IsValid()) {
                SetOrderFollowers(followers, entityId.EntityId, hex, current.Value, rate);
                isSetRate = true;
            }

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.Unit) && stronghold.IsValid()) {
                SetOrderFollowers(followers, entityId.EntityId, stronghold, current.Value, rate);
                isSetRate = true;
            }

            if (!isSetRate) {
                var diff = rate - team.TargetInfoSet.PowerRate;
                if (diff * diff > powerRateDiff * powerRateDiff) {
                    // set rate
                    var set = team.TargetInfoSet;
                    set.PowerRate = rate;
                    team.TargetInfoSet = set;
                    SetOrderFollowers(followers, entityId.EntityId, rate);
                }
            }

            team.IsNeedUpdate = 0;

            // オーダーが変わった場合のみ直接命令を下す
            if (changed)
                SetOrderFollowers(followers, entityId.EntityId, current.Value);
        }

        const float powerRateDiff = 0.1f;
        const float powerRateMin = 0.3f;

        private OrderType? GetOrder(UnitSide side, in Vector3 pos, float length, out float rate)
        {
            float ally = 0.0f;
            float enemy = 0.0f;
            rate = 1.0f;

            var colls = Physics.OverlapSphere(pos, length, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                BaseUnitStatus.Component? unit;
                if (TryGetComponent(comp.EntityId, out unit))
                {
                    if (unit.Value.State == UnitState.Dead)
                        continue;

                    var power = 0.0f;
                    switch (unit.Value.Type)
                    {
                        case UnitType.Soldier:
                        case UnitType.Commander:
                            power = 1.0f;
                            break;

                        case UnitType.Advanced:
                            power = 5.0f;
                            break;

                        case UnitType.Turret:
                            power = 3.0f;
                            break;
                    }

                    // todo calc war power
                    if (unit.Value.Side == side)
                        ally += power;
                    else if (unit.Value.Side != UnitSide.None)
                        enemy += power;
                }
            }

            if (ally > 0)
                rate = Mathf.Max(enemy / ally, powerRateMin);

            if (ally > enemy * AttackLogicDictionary.JudgeRate)
                return OrderType.Attack;

            return null;
        }
        #endregion

        #region SetMethod
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, OrderType order)
        {
            foreach (var id in followers)
            {
                base.SetOrder(id, order);
            }
            base.SetOrder(entityId, order);
        }

        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in UnitBaseInfo targetInfo, OrderType order, float rate)
        {
            foreach (var id in followers)
            {
                SetCommand(id, targetInfo, order, rate);
            }

            SetCommand(entityId, targetInfo, order, rate);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in FrontLineInfo lineInfo, OrderType order, float rate)
        {
            foreach (var id in followers)
            {
                SetCommand(id, lineInfo, order, rate);
            }

            SetCommand(entityId, lineInfo, order, rate);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in HexBaseInfo hexInfo, OrderType order, float rate)
        {
            foreach (var id in followers)
            {
                SetCommand(id, hexInfo, order, rate);
            }

            SetCommand(entityId, hexInfo, order, rate);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, float rate)
        {
            foreach (var id in followers)
            {
                SetCommand(id, rate);
            }

            SetCommand(entityId, rate);
        }
        private void SetCommand(EntityId id, in UnitBaseInfo unitBaseInfo, OrderType order, float rate)
        {
            base.SetOrder(id, order);
            this.UpdateSystem.SendEvent(new BaseUnitTarget.SetTarget.Event(new TargetInfo(unitBaseInfo, rate)), id);
        }
        private void SetCommand(EntityId id, in FrontLineInfo lineInfo, OrderType order, float rate)
        {
            base.SetOrder(id, order);
            this.UpdateSystem.SendEvent(new BaseUnitTarget.SetFrontLine.Event(new TargetFrontLineInfo(lineInfo,rate)), id);
        }
        private void SetCommand(EntityId id, in HexBaseInfo hexInfo, OrderType order, float rate)
        {
            base.SetOrder(id, order);
            this.UpdateSystem.SendEvent(new BaseUnitTarget.SetHex.Event(new TargetHexInfo(hexInfo, rate)), id);
        }
        private void SetCommand(EntityId id, float rate)
        {
            this.UpdateSystem.SendEvent(new BaseUnitTarget.SetPowerRate.Event(new TargetPowerRate(rate)), id);
        }
        #endregion
    }

    public abstract class BaseCommanderSearch : BaseSearchSystem
    {
        #region CheckMethod
        protected bool CheckNeedsFollowers(ref CommanderTeam.Component commander, int soldiers, int commanders, uint rank)
        {
            if (GetFollowerCount(ref commander,false) < soldiers)
                return true;

            if (rank > 0 &&
                GetFollowerCount(ref commander, true) <= commanders)
                return true;

            return false;
        }

        protected int GetFollowerCount(CommanderTeam.Component commander, bool isUnderCommander)
        {
            if (isUnderCommander)
                return commander.FollowerInfo.UnderCommanders.Count(f => CheckAlive(f.Id));
            else
                return commander.FollowerInfo.Followers.Count(f => CheckAlive(f.Id));
        }

        protected int GetFollowerCount(ref CommanderTeam.Component commander, bool isUnderCommander)
        {
            var info = commander.FollowerInfo;
            List<EntityId> followers;

            if (isUnderCommander)
                followers = info.UnderCommanders;
            else
                followers = info.Followers;

            //var list = followers.Where(f => HasEntity(f)).ToList();

            //if (isUnderCommander)
            //    info.UnderCommanders = list;
            //else
            //    info.Followers = list;
            //
            //commander.FollowerInfo = info;

            return followers.Count(f => CheckAlive(f.Id));//list.Count(f => CheckAlive(f.Id));
        }
        #endregion
    }
}
