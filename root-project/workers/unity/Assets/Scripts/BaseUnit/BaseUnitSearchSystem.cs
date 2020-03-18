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
        IntervalChecker interval;
        const int frequency = 15; 

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadWrite<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.ComponentAuthority>(),
                ComponentType.ReadOnly<GunComponent.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
            group.SetFilter(BaseUnitTarget.ComponentAuthority.Authoritative);

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f / frequency);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitAction.Component action,
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

                // initial
                target.State = TargetState.None;
                action.EnemyPositions.Clear();

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                UnitInfo enemy = null;
                var sightRange = action.SightRange;
                
                //if (status.Order != OrderType.Escape)
                enemy = getNearestEnemy(status.Side, pos, sightRange);

                if (enemy == null) {
                    if (target.TargetInfo.IsTarget) {
                        movement.TargetPosition = target.TargetInfo.Position;

                        var diff = movement.TargetPosition.ToUnityVector() - pos;
                        var s_range = RangeDictionary.SightRangeRate * sightRange;
                        if (diff.sqrMagnitude <= s_range * s_range)
                            target.State = TargetState.MovementTarget;
                        else
                            target.State = TargetState.OutOfRange;
                    }
                }
                else {
                    target.State = TargetState.ActionTarget;
                    var epos = enemy.pos.ToWorldPosition(this.Origin);

                    movement.TargetPosition = epos;
                    action.EnemyPositions.Add(epos);
                }

                var targetInfo = target.TargetInfo;
                var entityId = targetInfo.CommanderId;
                Position.Component? comp = null;
                if (entityId.IsValid() && base.TryGetComponent<Position.Component>(entityId, out comp))
                    movement.CommanderPosition = comp.Value.Coords.ToUnityVector().ToFixedPointVector3();
                else
                    movement.CommanderPosition = FixedPointVector3.Zero;

                var range = gun.GetAttackRange();
                if (status.Type == UnitType.Commander && targetInfo.Side != status.Side) {
                    if (targetInfo.Type == UnitType.Stronghold &&
                        (targetInfo.Side == UnitSide.None || targetInfo.State == UnitState.Dead)) {
                        range = GetDominationRange(targetInfo.TargetId) / 2;
                    }
                    else {
                        range += targetInfo.AllyRange;
                    }
                }

                range = AttackLogicDictionary.GetOrderRange(status.Order, range);
                movement.TargetRange = range;
            });
        }

        readonly Dictionary<EntityId,float> dominationRangeDic = new Dictionary<EntityId, float>();
        private float GetDominationRange(EntityId entityId)
        {
            if (dominationRangeDic.ContainsKey(entityId) == false) {
                if (TryGetComponent<DominationStamina.Component>(entityId, out var comp) == false)
                    return 1.0f;

                dominationRangeDic.Add(entityId, comp.Value.Range);
            }

            return dominationRangeDic[entityId];
        }
    }

    public abstract class BaseSearchSystem : BaseEntitySearchSystem
    {
        protected UnitInfo getUnitInfo(EntityId entityId)
        {
            BaseUnitStatus.Component? unit;
            if (TryGetComponent(entityId, out unit) == false)
                return null;

            Transform trans;
            if (TryGetComponentObject(entityId, out trans) == false)
                return null;

            var info = new UnitInfo();
            info.id = entityId;
            info.pos = trans.position;
            info.rot = trans.rotation;
            info.type = unit.Value.Type;
            info.side = unit.Value.Side;
            info.order = unit.Value.Order;
            info.state = unit.Value.State;

            return info;
        }

        protected UnitInfo getNearestEnemy(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, true, null, allowDead, types);
        }

        protected UnitInfo getNearestAlly(EntityId selfId, UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, false, selfId, allowDead, types);
        }

        protected UnitInfo getNearestUnit(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId, bool allowDead = false, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, isEnemy, selfId, allowDead, isPlayer:false, types);
        }

        protected UnitInfo getNearestPlayer(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId = null, bool allowDead = false, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, isEnemy, selfId, allowDead, isPlayer:true, types);
        }

        protected UnitInfo getNearestPlayer(in Vector3 pos, float length, EntityId? selfId = null, params UnitType[] types)
        {
            return getNearestUnit(null, pos, length, false, selfId, allowDead:true, isPlayer:true, types);
        }

        protected UnitInfo getNearestUnit(UnitSide? self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId, bool allowDead, bool isPlayer, params UnitType[] types)
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

                if (isPlayer && !TryGetComponent<PlayerInfo.Component>(comp.EntityId, out var player))
                    continue;

                BaseUnitStatus.Component? unit;
                if (TryGetComponent(comp.EntityId, out unit))
                {
                    if (unit.Value.State == UnitState.Dead && allowDead == false)
                        continue;

                    if (self_side != null && ((unit.Value.Side == self_side.Value) == isEnemy))
                        continue;

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var l = (col.transform.position - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        info = info ?? new UnitInfo();
                        info.id = comp.EntityId;
                        info.pos = col.transform.position;
                        info.rot = col.transform.rotation;
                        info.type = unit.Value.Type;
                        info.side = unit.Value.Side;
                        info.state = unit.Value.State;
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
        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, isEnemy:false, allowDead, types);
        }

        /// <summary>
        /// Get Ally UnitsInfo. allowDead = false;
        /// </summary>
        /// <param name="self_side"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected List<UnitInfo> getEnemyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, isEnemy: true, allowDead, types);
        }

        protected List<UnitInfo> getAllUnits(in Vector3 pos, float length, params UnitType[] types)
        {
            return getUnits(UnitSide.None, pos, length, isEnemy: null, allowDead: false, types);
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
                    info.rot = col.transform.rotation;
                    info.type = unit.Value.Type;
                    info.side = unit.Value.Side;
                    info.order = unit.Value.Order;
                    info.state = unit.Value.State;

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

        protected bool SetCommand(EntityId id, OrderType order, Entity? sendingEntity = null)
        {
            BaseUnitStatus.Component? status;
            if (base.TryGetComponent(id, out status) == false)
                return false;

            if (status.Value.Order == order)
                return false;

            var send = sendingEntity ?? Entity.Null;
            this.CommandSystem.SendCommand(new BaseUnitStatus.SetOrder.Request(id, new OrderInfo() { Order = order }), send);
            return true;
        }
    }

    // Utils
    public class UnitInfo
    {
        public EntityId id;
        public Vector3 pos;
        public Quaternion rot;
        public UnitType type;
        public UnitSide side;
        public OrderType order;
        public UnitState state;
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
