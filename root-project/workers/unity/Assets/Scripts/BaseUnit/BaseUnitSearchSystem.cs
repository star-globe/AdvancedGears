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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class BaseUnitSearchSystem : BaseSearchSystem
    {
        EntityQuery group;
        IntervalChecker inter;
        const int frequency = 20; 

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitSight.HasAuthority>(),
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadWrite<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.HasAuthority>(),
                ComponentType.ReadOnly<GunComponent.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(frequency);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitAction.Component action,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component target,
                                          ref GunComponent.Component gun,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (UnitUtils.IsWatcher(status.Type) == false)
                    return;

                // initial
                target.State = TargetState.None;
                action.EnemyPositions.Clear();

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                UnitInfo enemy = null;
                var sightRange = action.SightRange;

                var backBuffer = sightRange / 2;
                if (status.Type == UnitType.Commander)
                    backBuffer += RangeDictionary.AllyRange / 2;

                // strategy target
                SetStrategyTarget(pos, backBuffer, ref sight, ref target);

                // keep logic
                if (status.Order == OrderType.Keep)
                    sightRange *= target.PowerRate * target.PowerRate;

                enemy = getNearestEnemy(status.Side, pos, sightRange);

                if (enemy != null)
                {
                    var tgtPos = sight.TargetPosition.ToWorkerPosition(this.Origin);
                    var epos = enemy.pos.ToWorldPosition(this.Origin);

                    if (Vector3.Dot(tgtPos - pos, enemy.pos - pos) > 0)
                    {
                        target.State = TargetState.ActionTarget;
                        sight.TargetPosition = epos;
                    }

                    action.EnemyPositions.Add(epos);
                }

                float range;
                switch (target.State)
                {
                    case TargetState.ActionTarget:
                        range = gun.GetAttackRange();
                        range = AttackLogicDictionary.GetOrderRange(status.Order, range) * target.PowerRate;
                        break;

                    default:
                        range = RangeDictionary.BodySize;
                        break;
                }

                // set behind
                if (status.Type == UnitType.Commander) {
                    var addRange = RangeDictionary.AllyRange / 2;
                    range += AttackLogicDictionary.RankScaled(addRange, status.Rank);
                }

                sight.TargetRange = range;
                sight.State = target.State;
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

        private TargetState CalcTargetState(Vector3 diff, float sightRange)
        {
            var s_range = RangeDictionary.SightRangeRate * sightRange;
            if (diff.sqrMagnitude <= s_range * s_range)
                return TargetState.MovementTarget;
            else
                return TargetState.OutOfRange;
        }

        private bool SetStrategyTarget(Vector3 pos, float backBuffer, ref BaseUnitSight.Component sight, ref BaseUnitTarget.Component target)
        {
            bool isTarget = false;
            switch (target.Type)
            {
                case TargetType.Unit:
                    if (target.TargetUnit.IsValid())
                    {
                        sight.TargetPosition = target.TargetUnit.Position.ToFixedPointVector3();
                        target.State = TargetState.MovementTarget;
                        isTarget = true;
                    }
                    else
                        Debug.LogError("FrontLineInfo is InValid");
                    break;

                case TargetType.FrontLine:
                    if (target.FrontLine.IsValid()) {
                        sight.TargetPosition = target.FrontLine.GetOnLinePosition(this.Origin, pos, -backBuffer).ToWorldPosition(this.Origin);
                        target.State = TargetState.MovementTarget;
                        isTarget = true;
                    }
                    else
                        Debug.LogError("FrontLineInfo is InValid");
                    break;

                case TargetType.Hex:
                    if (target.HexInfo.IsValid()) {
                        sight.TargetPosition = HexUtils.GetHexCenter(this.Origin, target.HexInfo.HexIndex, HexDictionary.HexEdgeLength).ToWorldPosition(this.Origin);
                        target.State = TargetState.MovementTarget;
                        isTarget = true;
                    }
                    else
                        Debug.LogError("HexInfo is InValid");
                    break;
            }

            return isTarget;
        }
    }

    public abstract class BaseSearchSystem : BaseEntitySearchSystem
    {
        readonly UnitInfo baseInfo = new UnitInfo();

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
            return getNearestUnit(self_side, pos, length, isEnemy, containsNone:false, selfId, allowDead, isPlayer:false, types);
        }

        protected UnitInfo getNearestPlayer(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId = null, bool allowDead = false, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, isEnemy, containsNone:false, selfId, allowDead, isPlayer:true, types);
        }

        protected UnitInfo getNearestPlayer(in Vector3 pos, float length, EntityId? selfId = null, params UnitType[] types)
        {
            return getNearestUnit(null, pos, length, isEnemy:false, containsNone:true, selfId, allowDead:true, isPlayer:true, types);
        }

        protected UnitInfo getNearestUnit(UnitSide? self_side, in Vector3 pos, float length, bool isEnemy, bool containsNone, EntityId? selfId, bool allowDead, bool isPlayer, params UnitType[] types)
        {
            float len = float.MaxValue;
            bool tof = false;
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

                    if (self_side != null) {
                        if (!containsNone && (unit.Value.Side == UnitSide.None))
                            continue;

                        if ((unit.Value.Side == self_side.Value) == isEnemy)
                            continue;
                    }

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var l = (col.transform.position - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        baseInfo.id = comp.EntityId;
                        baseInfo.pos = col.transform.position;
                        baseInfo.rot = col.transform.rotation;
                        baseInfo.type = unit.Value.Type;
                        baseInfo.side = unit.Value.Side;
                        baseInfo.state = unit.Value.State;

                        tof = true;
                    }
                }
            }

            return tof ? baseInfo: null;
        }

        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, isEnemy: false, allowDead:false, null, types);
        }

        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, isEnemy: false, allowDead, null, types);
        }

        /// <summary>
        /// Get Ally UnitsInfo. allowDead = false
        /// </summary>
        /// <param name="self_side"></param>
        /// <param name="pos"></param>
        /// <param name="length"></param>
        /// <param name="allowDead"></param>
        /// <param name="selfId"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, EntityId? selfId = null, params UnitType[] types)
        {
            return getUnits(self_side, pos, length, isEnemy:false, allowDead, selfId, types);
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
            return getUnits(self_side, pos, length, isEnemy: true, allowDead, null, types);
        }

        protected List<UnitInfo> getAllUnits(in Vector3 pos, float length, EntityId? selfId, bool allowDead = false, params UnitType[] types)
        {
            return getUnits(UnitSide.None, pos, length, isEnemy: null, allowDead, selfId, types);
        }

        protected List<UnitInfo> getAllUnits(in Vector3 pos, float length, bool allowDead = false, params UnitType[] types)
        {
            return getUnits(UnitSide.None, pos, length, isEnemy: null, allowDead, null, types);
        }

        protected List<UnitInfo> getUnits(UnitSide self_side, in Vector3 pos, float length, bool? isEnemy, bool allowDead, EntityId? selfId, params UnitType[] types)
        {
            List<UnitInfo> unitList = new List<UnitInfo>();

            var colls = Physics.OverlapSphere(pos, length, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                if (selfId != null && selfId.Value == comp.EntityId)
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
                    info.rank = unit.Value.Rank;

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

        protected bool SetOrder(EntityId id, OrderType order, Entity? sendingEntity = null)
        {
            BaseUnitStatus.Component? status;
            if (base.TryGetComponent(id, out status) == false)
                return false;

            if (status.Value.Order == order)
                return false;

            var send = sendingEntity ?? Entity.Null;
            this.UpdateSystem.SendEvent(new BaseUnitStatus.SetOrder.Event(new OrderInfo() { Order = order }), id);
            return true;
        }

        protected Vector3 GetHexCenter(uint index)
        {
            return HexUtils.GetHexCenter(this.Origin, index, HexDictionary.HexEdgeLength);
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
        public uint rank;
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
