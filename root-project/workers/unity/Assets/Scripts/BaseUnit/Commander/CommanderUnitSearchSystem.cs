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
        private EntityQuery targettingGroup;
        private EntityQuery teamingGroup;

        IntervalChecker inter;

        #region ComponentSystem
        protected override void OnCreate()
        {
            base.OnCreate();

            targettingGroup = GetEntityQuery(
                ComponentType.ReadWrite<CommanderSight.Component>(),
                ComponentType.ReadWrite<CommanderStatus.Component>(),
                ComponentType.ReadWrite<CommanderAction.Component>(),
                ComponentType.ReadOnly<CommanderAction.ComponentAuthority>(),
                ComponentType.ReadOnly<CommanderTeam.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            targettingGroup.SetFilter(CommanderAction.ComponentAuthority.Authoritative);

            teamingGroup = GetEntityQuery(
                ComponentType.ReadWrite<CommanderTeam.Component>(),
                ComponentType.ReadOnly<CommanderTeam.ComponentAuthority>(),
                ComponentType.ReadOnly<CommanderSight.Component>(),
                ComponentType.ReadOnly<CommanderStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            teamingGroup.SetFilter(CommanderTeam.ComponentAuthority.Authoritative);

            inter = IntervalCheckerInitializer.InitializedChecker(5.0f);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime())
                HandleTeaming();

            HandleTargetting();
        }
        private void HandleTargetting()
        {
            Entities.With(targettingGroup).ForEach((Entity entity,
                              ref CommanderSight.Component sight,
                              ref CommanderStatus.Component commander,
                              ref CommanderTeam.Component team,
                              ref CommanderAction.Component action,
                              ref BaseUnitStatus.Component status,
                              ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Commander)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                var inter = sight.Interval;
                if (inter.CheckTime() == false)
                    return;

                sight.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                bool is_target;
                //int sol = commander.TeamConfig.Soldiers / 2;
                //int com = commander.TeamConfig.Commanders / 2;
                //if (CheckNeedsFollowers(ref team, sol, com, commander.Rank))
                //    is_target = escapeOrder(status, entityId, pos, ref sight, ref commander);
                //else
                is_target = attackOrder(status, entityId, pos, ref sight, ref commander, ref team);

                action.IsTarget = is_target;
            });
        }
        
        private void HandleTeaming()
        {
            Entities.With(teamingGroup).ForEach((Entity entity,
                              ref CommanderSight.Component sight,
                              ref CommanderStatus.Component commander,
                              ref CommanderTeam.Component team,
                              ref BaseUnitStatus.Component status,
                              ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Commander)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (commander.Rank == 0)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                team.FollowerInfo.UnderCommanders.Clear();

                var rank = commander.Rank - 1;
                var allies = getAllyUnits(status.Side, pos, sight.Range, UnitType.Commander);
                foreach(var unit in allies) {
                    if (unit.id == entityId.EntityId)
                        continue;

                    CommanderStatus.Component? com;
                    if (this.TryGetComponent(unit.id, out com) == false)
                        continue;
                    
                    if (com.Value.Rank != rank)
                        continue;

                    team.FollowerInfo.UnderCommanders.Add(unit.id);
                }
            });
        }
        #endregion

        void commonTargeting(UnitInfo tgt, in SpatialEntityId entityId, in CommanderStatus.Component commander,
                            ref CommanderSight.Component sight, out TargetInfo targetInfo)
        {
            TargetBaseInfo baseInfo; 
            baseInfo.IsTarget = tgt != null;
            var tpos = FixedPointVector3.Zero;
            var type = UnitType.None;
            var side = UnitSide.None;
            var id = new EntityId();
            if (baseInfo.IsTarget)
            {
                id = tgt.id;
                tpos = tgt.pos.ToWorldPosition(this.Origin);
                type = tgt.type;
                side = tgt.side;
            }

            baseInfo.TargetId = id;
            baseInfo.Position = tpos;
            baseInfo.Type = type;
            baseInfo.Side = side;

            sight.TargetInfo = baseInfo;

            targetInfo = new TargetInfo(baseInfo.IsTarget,
                                        baseInfo.TargetId,
                                        baseInfo.Position,
                                        baseInfo.Type,
                                        baseInfo.Side,
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

        bool attackOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos,
                         ref CommanderSight.Component sight,
                         ref CommanderStatus.Component commander,
                         ref CommanderTeam.Component team)
        {
            // check rank
            UnitInfo tgt;
            var info = team.TargetStronghold;
            if (info.StrongholdId.IsValid()) {
                tgt = new UnitInfo() {
                    id = info.StrongholdId,
                    pos = info.Position.ToUnityVector() + this.Origin,
                    side = info.Side,
                    type = UnitType.Stronghold
                };

                Debug.LogFormat("TargetStronghold is Valid. Side:{0} Position:{1}", status.Side, tgt.pos);
            }
            else
                tgt = getNearestEnemey(status.Side, pos, sight.Range, UnitType.Stronghold, UnitType.Commander);

            TargetInfo targetInfo;
            commonTargeting(tgt, entityId, commander, ref sight, out targetInfo);

            // check power
            var current = GetOrder(status.Side, pos, sight.Range);
            if (current == null)
            {
                current = commander.Order.Upper == OrderType.Guard ?
                            OrderType.Guard : OrderType.Keep;
            }

            commander.Order.Self(current.Value);

            SetOrderFollowers(team.FollowerInfo.GetAllFollowers(), targetInfo, current.Value);

            return tgt != null;
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

                    if (unit.Value.Side == side)
                        ally += 1.0f;
                    else
                        enemy += 1.0f;
                }
            }

            float rate = 1.1f;
            if (ally > enemy * rate)
                return OrderType.Attack;

            //if (ally * rate * rate < enemy)
            //    return OrderType.Escape;

            return null;//OrderType.Keep;
        }
        #endregion

        #region SetMethod
        private void SetOrderFollowers(List<EntityId> followers, in TargetInfo targetInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, targetInfo, order);
            }

            SetCommand(targetInfo.CommanderId, targetInfo, order);
        }

        private bool SetCommand(EntityId id, in TargetInfo targetInfo, OrderType order)
        {
            if (base.SetCommand(id, order) == false)
                return false;
        
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(id, targetInfo));
            return true;
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

            var list = followers.Where(f => HasEntity(f)).ToList();

            if (isUnderCommander)
                info.UnderCommanders = list;
            else
                info.Followers = list;

            commander.FollowerInfo = info;

            return list.Count(f => CheckAlive(f.Id));
        }
        #endregion
    }
}
