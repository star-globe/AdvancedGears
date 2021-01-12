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
                                                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
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
                              ref BaseUnitTarget.Component target,
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

                applyOrder(status, entityId, pos, target.TargetInfo, action.SightRange, ref commander, ref team);
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
            TargetBaseInfo baseInfo; 
            baseInfo.IsTarget = tgt != null;
            var tpos = FixedPointVector3.Zero;
            var type = UnitType.None;
            var side = UnitSide.None;
            var state = UnitState.None;
            var id = new EntityId();
            if (baseInfo.IsTarget)
            {
                id = tgt.id;
                tpos = tgt.pos.ToWorldPosition(this.Origin);
                type = tgt.type;
                side = tgt.side;
                state = tgt.state;
            }

            baseInfo.TargetId = id;
            baseInfo.Position = tpos;
            baseInfo.Type = type;
            baseInfo.Side = side;
            baseInfo.State = state;

            targetInfo = new TargetInfo(baseInfo.IsTarget,
                                        baseInfo.TargetId,
                                        baseInfo.Position,
                                        baseInfo.Type,
                                        baseInfo.Side,
                                        baseInfo.State,
                                        entityId.EntityId,
                                        commander.AllyRange);
        }

        #region OrderMethod
        //bool escapeOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, ref CommanderSight.Component sight, ref CommanderStatus.Component commander)
        //{
        //    var tgt = getNearestAlly(status.Side, pos, sight.Range, UnitType.Stronghold);
        //    TargetInfo targetInfo;
        //    commonTargeting(tgt, entityId, commander, ref sight, out targetInfo);
        //
        //    commander.Order = commander.Order.Self(OrderType.Escape);
        //
        //    SetCommand(targetInfo.CommanderId, targetInfo, commander.Order.Self);
        //
        //    return tgt != null;
        //}

        void applyOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, in TargetInfo currentTarget, float sightRange,
                         ref CommanderStatus.Component commander,
                         ref CommanderTeam.Component team)
        {
            // check rank
            UnitInfo tgt;

            var scaledRange = AttackLogicDictionary.RankScaled(sightRange, status.Rank);

            //tgt = getNearestEnemy(status.Side, pos, scaledRange, allowDead: true, UnitType.Stronghold, UnitType.Commander);

            //TargetInfo targetInfo;
            //commonTargeting(tgt, entityId, commander, out targetInfo);

            // check power
            var current = GetOrder(status.Side, pos, scaledRange);
            if (current == null)
            {
                current = commander.Order.Upper == OrderType.Guard ?
                            OrderType.Guard : OrderType.Keep;
            }

            var changed = commander.Order.Self(current.Value);

            var line = team.TargetInfoSet.FrontLine;
            var hex = team.TargetInfoSet.HexInfo;
            var stronghold = team.TargetInfoSet.Stronghold;
            var followers = team.FollowerInfo.GetAllFollowers();

            var targetBit = team.IsNeedUpdate;

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.FrontLine) && line.FrontLine.IsValid())
                SetOrderFollowers(followers, entityId.EntityId, line, current.Value);

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.Hex) && hex.IsValid())
                SetOrderFollowers(followers, entityId.EntityId, hex, current.Value);

            if (UnitUtils.IsNeedUpdate(targetBit, TargetType.Stronghold) && stronghold.IsValid())
                SetOrderFollowers(followers, entityId.EntityId, stronghold, current.Value);

            team.IsNeedUpdate = 0;

            // ターゲットを直接指定する方式はやめる
            //if (targetInfo.Equals(currentTarget) == false || changed)
            //    SetOrderFollowers(followers, entityId.EntityId, targetInfo, current.Value);

            // オーダーが変わった場合のみ直接命令を下す
            if (changed)
                SetOrderFollowers(followers, entityId.EntityId, current.Value);
        }

        private OrderType? GetOrder(UnitSide side, in Vector3 pos, float length)
        {
            float ally = 0.0f;
            float enemy = 0.0f;

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

                    // todo calc war power
                    if (unit.Value.Side == side)
                        ally += 1.0f;
                    else
                        enemy += 1.0f;
                }
            }

            float rate = AttackLogicDictionary.JudgeRate;
            if (ally > enemy * rate)
                return OrderType.Attack;

            //if (ally * rate * rate < enemy)
            //    return OrderType.Escape;

            return null;//OrderType.Keep;
        }
        #endregion

        #region SetMethod
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, OrderType order)
        {
            foreach (var id in followers)
            {
                base.SetCommand(id, order);
            }
            base.SetCommand(entityId, order);
        }

        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in TargetInfo targetInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, targetInfo, order);
            }

            SetCommand(entityId, targetInfo, order);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in TargetFrontLineInfo lineInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, lineInfo, order);
            }

            SetCommand(entityId, lineInfo, order);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in TargetHexInfo hexInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, hexInfo, order);
            }

            SetCommand(entityId, hexInfo, order);
        }
        private void SetOrderFollowers(List<EntityId> followers, in EntityId entityId, in TargetStrongholdInfo strongholdInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, strongholdInfo, order);
            }

            SetCommand(entityId, strongholdInfo, order);
        }
        private void SetCommand(EntityId id, in TargetInfo targetInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(id, targetInfo));
        }
        private void SetCommand(EntityId id, in TargetFrontLineInfo lineInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetFrontLine.Request(id, lineInfo));
        }
        private void SetCommand(EntityId id, in TargetHexInfo hexInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetHex.Request(id, hexInfo));
        }
        private void SetCommand(EntityId id, in TargetStrongholdInfo strongholdInfo, OrderType order)
        {
            base.SetCommand(id, order);
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetStronghold.Request(id, strongholdInfo));
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
