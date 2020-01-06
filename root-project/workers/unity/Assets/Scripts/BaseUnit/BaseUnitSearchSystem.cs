using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class BaseUnitSearchSystem : BaseSearchSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.ReadWrite<BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<GunComponent.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitAction.Component action,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component target,
                                          ref GunComponent.Component gun) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (status.Type != UnitType.Soldier &&
                    status.Type != UnitType.Commander)
                    return;

                var inter = sight.Interval;
                if (inter.CheckTime() == false)
                    return;

                sight.Interval = inter;

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;
                action.EnemyPositions.Clear();

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                UnitInfo enemy = null;
                //if (status.Order != OrderType.Escape)
                enemy = getNearestEnemey(status.Side, pos, sight.Range);

                if (enemy == null)
                {
                    if (target.TargetInfo.IsTarget)
                    {
                        movement.TargetPosition = target.TargetInfo.Position;
                        movement.IsTarget = true;
                    }
                }
                else
                {
                    movement.IsTarget = true;
                    action.IsTarget = true;
                    var epos = enemy.pos.ToWorldPosition(this.Origin);

                    movement.TargetPosition = epos;
                    action.EnemyPositions.Add(epos);
                }

                var entityId = target.TargetInfo.CommanderId;
                Position.Component? comp = null;
                if (entityId.IsValid() && base.TryGetComponent<Position.Component>(entityId, out comp))
                {
                    movement.CommanderPosition = comp.Value.Coords.ToUnityVector().ToFixedPointVector3();
                }
                else
                    movement.CommanderPosition = FixedPointVector3.Zero;

                var range = gun.GetAttackRange();
                if (status.Type == UnitType.Commander && target.TargetInfo.Side != status.Side)
                {
                    switch (target.TargetInfo.Type)
                    {
                        case UnitType.Commander:
                        case UnitType.Stronghold: range += target.TargetInfo.AllyRange; break;
                    }
                }

                switch (status.Order)
                {
                    case OrderType.Move:
                    case OrderType.Guard: range *= 0.4f; break;
                    case OrderType.Attack: range *= 0.8f; break;
                    case OrderType.Keep: range *= 1.5f; break;
                }

                movement.TargetRange = range;
            });
        }
    }

    public abstract class BaseSearchSystem : BaseEntitySearchSystem
    {
        protected UnitInfo getNearestEnemey(UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, true, null, types);
        }

        protected UnitInfo getNearestAlly(EntityId selfId, UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, false, selfId, types);
        }

        protected UnitInfo getNearestUnit(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId, params UnitType[] types)
        {
            float len = float.MaxValue;
            UnitInfo info = null;

            var colls = Physics.OverlapSphere(pos, length, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                if (selfId != null && selfId.Value.Equals(comp.EntityId))
                    continue;

                BaseUnitStatus.Component? unit;
                if (TryGetComponent(comp.EntityId, out unit))
                {
                    if (unit.Value.State == UnitState.Dead)
                        continue;

                    if ((unit.Value.Side == self_side) == isEnemy)
                        continue;

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var t_pos = col.transform.position;
                    var l = (t_pos - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        info = info ?? new UnitInfo();
                        info.id = comp.EntityId;
                        info.pos = t_pos;
                        info.type = unit.Value.Type;
                        info.side = unit.Value.Side;
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// Get Ally UnitsInfo. allowDead = false;
        /// </summary>
        /// <param name="self_side"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, false, false, types);
        }

        protected List<UnitInfo> getUnits(UnitSide self_side, in Vector3 pos, float length, bool? isEnemy, bool allowDead, params UnitType[] types)
        {
            List<UnitInfo> unitList = new List<UnitInfo>();

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
                    if (unit.Value.State == UnitState.Dead && allowDead == false)
                        continue;

                    if (isEnemy != null && (unit.Value.Side == self_side) == isEnemy.Value)
                        continue;

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var info = new UnitInfo();
                    info.id = comp.EntityId;
                    info.pos = col.transform.position;
                    info.type = unit.Value.Type;
                    info.side = unit.Value.Side;
                    info.order = unit.Value.Order;

                    unitList.Add(info);
                }
            }

            return unitList;
        }

        protected bool CheckAlive(long entityId)
        {
            BaseUnitStatus.Component? status;
            if (TryGetComponent(new EntityId(entityId), out status) == false)
                return false;

            return status.Value.State == UnitState.Alive;
        }

        protected bool SetCommand(EntityId id, OrderType order, out Entity entity)
        {
            if (!base.TryGetEntity(id, out entity))
                return false;

            BaseUnitStatus.Component? status;
            if (base.TryGetComponent(id, out status) == false)
                return false;

            if (status.Value.Order == order)
                return false;

            this.CommandSystem.SendCommand(new BaseUnitStatus.SetOrder.Request(id, new OrderInfo() { Order = order }), entity);
            return true;
        }
    }

    // Utils
    public class UnitInfo
    {
        public EntityId id;
        public Vector3 pos;
        public UnitType type;
        public UnitSide side;
        public OrderType order;
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
