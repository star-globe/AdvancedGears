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
        private EntityQueryBuilder.F_EDDDDD<CommanderStatus.Component, CommanderTeam.Component, BaseUnitStatus.Component, UnitActionData, SpatialEntityId> targetAction;
        private EntityQuerySet teamingQuerySet;
        private EntityQueryBuilder.F_EDDDD<CommanderSight.Component, CommanderTeam.Component, BaseUnitStatus.Component, SpatialEntityId> teamAction;
        const float teaminTime = 1.0f;
        const int period = 10;

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
                                                    ComponentType.ReadOnly<UnitActionData>(),
                                                    ComponentType.ReadOnly<Transform>(),
                                                    ComponentType.ReadOnly<SpatialEntityId>()
                                                    ), period);
            targetAction = TargetQuery;

            teamingQuerySet = new EntityQuerySet(GetEntityQuery(
                                                 ComponentType.ReadWrite<CommanderTeam.Component>(),
                                                 ComponentType.ReadOnly<CommanderTeam.HasAuthority>(),
                                                 ComponentType.ReadOnly<CommanderSight.Component>(),
                                                 ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                 ComponentType.ReadOnly<Transform>(),
                                                 ComponentType.ReadOnly<SpatialEntityId>()
                                                ), teaminTime);
            teamAction = TeamQuery;
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

            Entities.With(targettingQuerySet.group).ForEach(targetAction);
        }

        private void TargetQuery(Entity entity,
                              ref CommanderStatus.Component commander,
                              ref CommanderTeam.Component team,
                              ref BaseUnitStatus.Component status,
                              ref UnitActionData action,
                              ref SpatialEntityId entityId)
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
        }

        
        private void HandleTeaming()
        {
            if (CheckTime(ref teamingQuerySet.inter) == false)
                return;

            Entities.With(teamingQuerySet.group).ForEach(teamAction);
        }
        
        private void TeamQuery(Entity entity,
                              ref CommanderSight.Component sight,
                              ref CommanderTeam.Component team,
                              ref BaseUnitStatus.Component status,
                              ref SpatialEntityId entityId)
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

#if false
            team.FollowerInfo.UnderCommanders.Clear();

            var rank = status.Rank - 1;
            var range = RangeDictionary.GetBoidsRange(status.Rank);
            var allies = getAllyUnits(status.Side, pos, range, allowDead:false, GetSingleUnitTypes(UnitType.Commander));
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
#else
            var info = team.FollowerInfo;
            var solNum = AttackLogicDictionary.UnderSoldiers;

            bool changed = false;
            if (info.Followers.Count < solNum) {
                var allies = getAllyUnits(status.Side, pos, (float) RangeDictionary.TeamInter, allowDead: false, GetSingleUnitTypes(UnitType.Soldier));
                foreach (var unit in allies){
                    int index = -1;
                    foreach (var id in info.Followers) {
                        if (id == unit.id)
                            break;
                    }

                    if (index < 0) {
                        info.Followers.Add(unit.id);
                        changed = true;
                    }
                }
            }

            if (changed)
                team.FollowerInfo = info;
#endif
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
            baseInfo.Size = tgt.size;

            targetInfo = new TargetInfo(baseInfo,1.0f);
        }

        #region OrderMethod
        private readonly List<EntityId> allFollowers = new List<EntityId>();

        void applyOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, float sightRange,
                         ref CommanderStatus.Component commander,
                         ref CommanderTeam.Component team)
        {
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
            var followers = team.FollowerInfo.GetAllFollowers(allFollowers);

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
                var min = AttackLogicDictionary.PowerRateDiff;
                var diff = rate - team.TargetInfoSet.PowerRate;
                if (diff * diff > min * min) {
                    // set rate
                    var set = team.TargetInfoSet;
                    set.PowerRate = rate;
                    team.TargetInfoSet = set;
                    SetOrderFollowers(followers, entityId.EntityId, rate);
                }
            }

            team.IsNeedUpdate = 0;

            // if orde is changed, set orders
            if (changed)
                SetOrderFollowers(followers, entityId.EntityId, current.Value);
        }

        readonly Collider[] colls = new Collider[256];

        private OrderType? GetOrder(UnitSide side, in Vector3 pos, float length, out float rate)
        {
            float ally = 0.0f;
            float enemy = 0.0f;
            rate = AttackLogicDictionary.PowerRateMin;

            var count = Physics.OverlapSphereNonAlloc(pos, length, colls, this.UnitLayer);
            for (var i = 0; i < count; i++)
            {
                var col = colls[i];
                if (col.TryGetComponent<LinkedEntityComponent>(out var comp) ==false)
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
                rate = Mathf.Max(enemy * enemy / (ally * ally), AttackLogicDictionary.PowerRateMin);

            // Commander will not order Attack. Player or HQ orders Attack.

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

            return followers.Count(f => CheckAlive(f.Id));
        }
        #endregion
    }
}
