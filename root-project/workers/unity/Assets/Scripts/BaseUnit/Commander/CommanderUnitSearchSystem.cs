using System;
using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class CommanderUnitSearchSystem : BaseSearchSystem
    {
        private CommandSystem commandSystem;
        private EntityQuery group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            commandSystem = World.GetExistingSystem<CommandSystem>();
            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<CommanderSight.Component>(),
                ComponentType.ReadWrite<CommanderStatus.Component>(),
                ComponentType.ReadWrite<CommanderAction.Component>(),
                ComponentType.ReadOnly<CommanderAction.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(CommanderAction.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                              ref CommanderSight.Component sight,
                              ref CommanderStatus.Component commander,
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

                var time = Time.time;
                var inter = sight.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                sight.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                bool is_target;
                int num = 5;
                if (commander.NeedsFollowers(num))
                    is_target = escapeOrder(status, entityId, pos, ref sight, ref commander);
                else if (commander.SuperiorInfo.IsNeedToOrder())
                    is_target = organizeOrder(status.Side, pos, ref commander);
                else
                    is_target = attackOrder(status, entityId, pos, ref sight, ref commander);

                action.IsTarget = is_target;
            });
        }

        void commonTargeting(UnitInfo tgt, in SpatialEntityId entityId, in CommanderStatus.Component commander,
                            ref CommanderSight.Component sight, out TargetInfo targetInfo)
        {
            TargetBaseInfo baseInfo; 
            baseInfo.IsTarget = tgt != null;
            var tpos = Improbable.Vector3f.Zero;
            var type = UnitType.None;
            var side = UnitSide.None;
            var id = new EntityId();
            if (baseInfo.IsTarget)
            {
                id = tgt.id;
                tpos = new Improbable.Vector3f(tgt.pos.x - origin.x,
                                               tgt.pos.y - origin.y,
                                               tgt.pos.z - origin.z);
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

        bool escapeOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, ref CommanderSight.Component sight, ref CommanderStatus.Component commander)
        {
            var tgt = getNearestAlly(status.Side, pos, sight.Range, UnitType.Stronghold);
            TargetInfo targetInfo;
            commonTargeting(tgt, entityId, commander, ref sight, out targetInfo);

            commander.Order = commander.Order.Self(OrderType.Escape);

            SetCommand(targetInfo.CommanderId, targetInfo, commander.Order.Self);

            return tgt != null;
        }

        const float radioRange = 1000.0f;
        bool organizeOrder(UnitSide side, in Vector3 pos, ref CommanderStatus.Component commander)
        {
            var tgt = getNearestAlly(side, pos, radioRange, UnitType.HeadQuarter);

            commander.Order = commander.Order.Self(OrderType.Organize);
            commander.SuperiorInfo = commander.SuperiorInfo.SetIsOrder(true);

            return tgt != null;
        }

        bool attackOrder(in BaseUnitStatus.Component status, in SpatialEntityId entityId, in Vector3 pos, ref CommanderSight.Component sight, ref CommanderStatus.Component commander)
        {
            // check rank
            var tgt = getNearestEnemey(status.Side, pos, sight.Range, UnitType.Stronghold, UnitType.Commander);
            TargetInfo targetInfo;
            commonTargeting(tgt, entityId, commander, ref sight, out targetInfo);

            // check power
            OrderType current = GetOrder(status.Side, pos, sight.Range);
            commander.Order.Self(current);

            SetFollowers(commander.FollowerInfo.Followers, targetInfo, current);

            return tgt != null;
        }

        private OrderType GetOrder(UnitSide side, in Vector3 pos, float length)
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

            float rate = 1.3f;
            if (ally > enemy * rate)
                return OrderType.Attack;
            
            if (ally * rate * rate < enemy)
                return OrderType.Escape;

            return OrderType.Keep;
        }

        private void SetFollowers(List<EntityId> followers, in TargetInfo targetInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, targetInfo, order);
            }

            SetCommand(targetInfo.CommanderId, targetInfo, order);
        }

        private bool SetCommand(EntityId id, in TargetInfo targetInfo, OrderType order)
        {
            Entity entity;
            if (!base.TryGetEntity(id, out entity))
                return false;

            commandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(id, targetInfo), entity);

            BaseUnitStatus.Component? status;
            if (base.TryGetComponent(id, out status) == false)
                return false;

            if (status.Value.Order == order)
                return false;

            commandSystem.SendCommand(new BaseUnitStatus.SetOrder.Request(id, new OrderInfo(){ Order = order }), entity);
            return true;
        }
    }
}
