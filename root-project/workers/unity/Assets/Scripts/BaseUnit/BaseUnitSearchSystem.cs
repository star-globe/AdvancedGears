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
        EntityQueryBuilder.F_EDDDDD<BaseUnitSight.Component, UnitActionData, BaseUnitStatus.Component, BaseUnitTarget.Component, SpatialEntityId> action;
        IntervalChecker inter;
        const int frequency = 10;

        Dictionary<long, List<FixedPointVector3>> enemyPositionsContainer = new Dictionary<long, List<FixedPointVector3>>();
        public Dictionary<long, List<FixedPointVector3>> EnemyPositionsContainer => enemyPositionsContainer;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitSight.HasAuthority>(),
                ComponentType.ReadWrite<UnitActionData>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadWrite<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.HasAuthority>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(frequency);

            action = Query;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach(action);
        }
        
        private void Query (Entity entity,
                              ref BaseUnitSight.Component sight,
                              ref UnitActionData action,
                              ref BaseUnitStatus.Component status,
                              ref BaseUnitTarget.Component target,
                              ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Order == OrderType.Idle)
                return;

            if (UnitUtils.IsWatcher(status.Type) == false)
                return;

            // initial
            target.State = TargetState.None;

            var id = entityId.EntityId.Id;
            if (enemyPositionsContainer.ContainsKey(id) == false)
                enemyPositionsContainer[id] = new List<FixedPointVector3>();

            var enemyPositions = enemyPositionsContainer[id];
            enemyPositions.Clear();

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
                sightRange *= target.PowerRate;

            enemy = getNearestEnemy(status.Side, pos, sightRange);

            if (enemy != null)
            {
                var tgtPos = sight.TargetPosition.ToWorkerPosition(this.Origin);
                var epos = enemy.pos.ToWorldPosition(this.Origin);

                if (Vector3.Dot(tgtPos - pos, enemy.pos - pos) > 0)
                {
                    target.State = TargetState.ActionTarget;
                    sight.TargetPosition = epos;
                    sight.TargetSize = enemy.size;
                }

                enemyPositions.Add(epos);
            }

            float range;
            switch (target.State)
            {
                case TargetState.ActionTarget:
                    range = action.AttackRange;
                    range = AttackLogicDictionary.GetOrderRange(status.Order, range) * target.PowerRate;
                    break;

                default:
                    range = RangeDictionary.BodySize;
                    break;
            }

            // set behind
            if (status.Type == UnitType.Commander) {
                var addRange = RangeDictionary.AllyRange;
                range += AttackLogicDictionary.RankScaled(addRange, status.Rank);
            }

            sight.TargetRange = range;
            sight.State = target.State;
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
                        sight.TargetSize = target.TargetUnit.Size;
                        target.State = TargetState.MovementTarget;
                        isTarget = true;
                    }
                    else
                        Debug.LogError("FrontLineInfo is InValid");
                    break;

                case TargetType.FrontLine:
                    if (target.FrontLine.IsValid()) {
                        sight.TargetPosition = target.FrontLine.GetOnLinePosition(this.Origin, pos, -backBuffer).ToWorldPosition(this.Origin);
                        sight.TargetSize = 0.0f;
                        target.State = TargetState.MovementTarget;
                        isTarget = true;
                    }
                    else
                        Debug.LogError("FrontLineInfo is InValid");
                    break;

                case TargetType.Hex:
                    if (target.HexInfo.IsValid()) {
                        sight.TargetPosition = HexUtils.GetHexCenter(this.Origin, target.HexInfo.HexIndex, HexDictionary.HexEdgeLength).ToWorldPosition(this.Origin);
                        sight.TargetSize = HexDictionary.HexTargetRadius;
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
        int layer = int.MinValue;
        protected int UnitLayer
        {
            get
            {
                if (layer == int.MinValue)
                    layer = LayerMask.GetMask("Unit");

                return layer;
            }
        }

        int unitNavArea = 0;
        protected int WalkableNavArea
        {
            get
            {
                if (unitNavArea == 0)
                    unitNavArea = NavMeshUtils.GetNavArea("Walkable");

                return unitNavArea;
            }
        }

        readonly UnitInfo baseInfo = new UnitInfo();
        readonly Collider[] colls = new Collider[256];
        readonly List<UnitInfo> unitList = new List<UnitInfo>();
        readonly Queue<UnitInfo> unitQueue = new Queue<UnitInfo>();

        readonly List<UnitInfo> playerUnitList = new List<UnitInfo>();

        readonly Dictionary<UnitType, UnitType[]> singleTypes = new Dictionary<UnitType, UnitType[]>();

        EntityQuery playerGroup;
        private EntityQueryBuilder.F_CDDD<Transform, BaseUnitStatus.Component, PlayerInfo.Component, SpatialEntityId> playerAction;

        private void CreatePlayerQuery()
        {
            playerGroup = GetEntityQuery(ComponentType.ReadOnly<PlayerInfo.Component>(),
                                         ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                         ComponentType.ReadOnly<SpatialEntityId>(),
                                         ComponentType.ReadOnly<Transform>());

            playerAction = PlayerQuery;
        }

        protected void UpdatePlayerPosition()
        {
            if (playerAction == null)
                CreatePlayerQuery();

            for (var i = 0; i < playerUnitList.Count; i++)
                unitQueue.Enqueue(playerUnitList[i]);

            playerUnitList.Clear();
            Entities.With(playerGroup).ForEach(playerAction);
        }

        private void PlayerQuery(Transform transform,
                                 ref BaseUnitStatus.Component status,
                                 ref PlayerInfo.Component player,
                                 ref SpatialEntityId spatialId)
        {
            UnitInfo info = null;
            playerUnitList.Add(unitQueue.Count > 0 ? unitQueue.Dequeue() : new UnitInfo());

            var index = playerUnitList.Count - 1;

            info = playerUnitList[index];
            info.id = spatialId.EntityId;
            info.pos = transform.position;
            info.rot = transform.rotation;
            info.type = status.Type;
            info.side = status.Side;
            info.order = status.Order;
            info.state = status.State;
            info.rank = status.Rank;
        }

        protected UnitType[] GetSingleUnitTypes(UnitType unit)
        {
            if (singleTypes.TryGetValue(unit, out var types))
                return types;

            types = new UnitType[] { unit };
            singleTypes.Add(unit, types);
            return types;
        }

        protected UnitInfo getUnitInfo(EntityId entityId)
        {
            BaseUnitStatus.Component? unit;
            if (TryGetComponent(entityId, out unit) == false)
                return null;

            Transform trans;
            if (TryGetComponentObject(entityId, out trans) == false)
                return null;

            UnitTransform unitTransform;
            TryGetComponentObject(entityId, out unitTransform);

            var info = new UnitInfo();
            info.id = entityId;
            info.pos = unitTransform == null ? trans.position: unitTransform.Bounds.center;
            info.size = unitTransform == null ? 0.0f: unitTransform.SizeRadius;
            info.rot = trans.rotation;
            info.type = unit.Value.Type;
            info.side = unit.Value.Side;
            info.order = unit.Value.Order;
            info.state = unit.Value.State;

            return info;
        }

        protected UnitInfo getNearestEnemy(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, UnitType[] types = null)
        {
            return getNearestUnit(self_side, pos, length, true, null, allowDead, types);
        }

        protected UnitInfo getNearestAlly(EntityId selfId, UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, UnitType[] types = null)
        {
            return getNearestUnit(self_side, pos, length, false, selfId, allowDead, types);
        }

        protected UnitInfo getNearestUnit(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId, bool allowDead = false, UnitType[] types = null)
        {
            return getNearestUnit(self_side, pos, length, isEnemy, containsNone:false, selfId, allowDead, isPlayer:false, types);
        }

        //protected UnitInfo getNearestPlayer(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId = null, bool allowDead = false, UnitType[] types = null)
        //{
        //    return 
        //}

        protected UnitInfo getNearestPlayer(UnitSide? self_side, in Vector3 pos, float length, bool isEnemy, EntityId? selfId = null, bool allowDead = false)
        {
            float len = length * length;
            bool tof = false;
            foreach (var p in playerUnitList)
            {
                if (selfId != null && selfId.Value.Equals(p.id))
                    continue;

                if (p.state == UnitState.Dead && allowDead == false)
                    continue;

                if (self_side != null)
                {
                    if ((p.side == self_side.Value) == isEnemy)
                        continue;
                }

                var l = (p.pos - pos).sqrMagnitude;
                if (l < len)
                {
                    len = l;
                    baseInfo.id = p.id;
                    baseInfo.pos = p.pos;
                    baseInfo.rot = p.rot;
                    baseInfo.size = p.size;
                    baseInfo.type = p.type;
                    baseInfo.side = p.side;
                    baseInfo.state = p.state;

                    tof = true;
                }
            }

            return tof ? baseInfo : null;
        }

        protected UnitInfo getNearestPlayer(in Vector3 pos, float length, EntityId? selfId = null)
        {
            return getNearestPlayer(null, pos, length, isEnemy:false, selfId, allowDead:true);
        }

        protected UnitInfo getNearestUnit(UnitSide? self_side, in Vector3 pos, float length, bool isEnemy, bool containsNone, EntityId? selfId, bool allowDead, bool isPlayer, UnitType[] types = null)
        {
            float len = float.MaxValue;
            bool tof = false;

            var count = Physics.OverlapSphereNonAlloc(pos, length, colls, this.UnitLayer);
            for (var i = 0; i < count; i++)
            {
                // todo check CounterCache

                var col = colls[i];
                if (col.TryGetComponent<BaseUnitStatusInfoComponent>(out var comp) == false)
                    continue;

                if (selfId != null && selfId.Value.Equals(comp.EntityId))
                    continue;

                if (isPlayer && !TryGetComponent<PlayerInfo.Component>(comp.EntityId, out var player))
                    continue;

                if (comp.State == UnitState.Dead && allowDead == false)
                    continue;

                if (self_side != null) {
                    if (!containsNone && (comp.Side == UnitSide.None))
                        continue;

                    if ((comp.Side == self_side.Value) == isEnemy)
                        continue;
                }

                if (types != null && types.Length != 0) {
                    bool contains = false;
                    foreach (var t in types)
                        contains |= t == comp.Type;

                    if (!contains)
                        continue;
                }

                TryGetComponentObject<UnitTransform>(comp.EntityId, out var unit);

                var colPos = unit == null ? col.transform.position: unit.Bounds.center;
                var l = (colPos - pos).sqrMagnitude;
                if (l < len)
                {
                    len = l;
                    baseInfo.id = comp.EntityId;
                    baseInfo.pos = colPos;
                    baseInfo.rot = col.transform.rotation;
                    baseInfo.type = comp.Type;
                    baseInfo.side = comp.Side;
                    baseInfo.state = comp.State;
                    baseInfo.size = comp.Size;

                    tof = true;
                }
            }

            return tof ? baseInfo: null;
        }

        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length)
        {
            return getUnits(self_side, pos, length, isEnemy: false, allowDead:false, null, null);
        }

        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, UnitType[] types = null)
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
        protected List<UnitInfo> getAllyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, EntityId? selfId = null, UnitType[] types = null)
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
        protected List<UnitInfo> getEnemyUnits(UnitSide self_side, in Vector3 pos, float length, bool allowDead = false, UnitType[] types = null)
        {
            return getUnits(self_side, pos, length, isEnemy: true, allowDead, null, types);
        }

        protected List<UnitInfo> getAllUnits(in Vector3 pos, float length, EntityId? selfId, bool allowDead = false, UnitType[] types = null)
        {
            return getUnits(UnitSide.None, pos, length, isEnemy: null, allowDead, selfId, types);
        }

        protected List<UnitInfo> getAllUnits(in Vector3 pos, float length, bool allowDead = false, UnitType[] types = null)
        {
            return getUnits(UnitSide.None, pos, length, isEnemy: null, allowDead, null, types);
        }

        protected List<UnitInfo> getUnits(UnitSide self_side, in Vector3 pos, float length, bool? isEnemy, bool allowDead, EntityId? selfId, UnitType[] types)
        {
            var count = Physics.OverlapSphereNonAlloc(pos, length, colls, this.UnitLayer);
            return getUnitsFromColls(count, colls, self_side, isEnemy, allowDead, selfId, types);
        }

        protected List<UnitInfo> getUnitsFromCapsel(UnitSide self_side, in Vector3 point0, in Vector3 point1, float radius, bool? isEnemy, bool allowDead, EntityId? selfId, UnitType[] types)
        {
            var count = Physics.OverlapCapsuleNonAlloc(point0, point1, radius, colls, this.UnitLayer);
            return getUnitsFromColls(count, colls, self_side, isEnemy, allowDead, selfId, types);
        }

        private List<UnitInfo> getUnitsFromColls(int count, Collider[] colls, UnitSide self_side, bool? isEnemy, bool allowDead, EntityId? selfId, UnitType[] types)
        {
            UnityEngine.Profiling.Profiler.BeginSample("GetUnitsFromColls");
            int index = 0;
            for (var i = 0; i < count; i++)
            {
                var col = colls[i];
                if (col.TryGetComponent<BaseUnitStatusInfoComponent>(out var comp) == false)
                    continue;

                if (selfId != null && selfId.Value == comp.EntityId)
                    continue;

                if (comp.State == UnitState.Dead && allowDead == false)
                    continue;

                if (isEnemy != null && (comp.Side == self_side) == isEnemy.Value)
                    continue;

                if (types != null && types.Length != 0) {
                    bool contains = false;
                    foreach (var t in types) {
                        contains |= t == comp.Type;
                    }

                    if (!contains)
                        continue;
                }

                UnitInfo info = null;
                if (index >= unitList.Count)
                    unitList.Add(unitQueue.Count > 0 ? unitQueue.Dequeue(): new UnitInfo());

                TryGetComponentObject<UnitTransform>(comp.EntityId, out var unit);

                info = unitList[index];
                info.id = comp.EntityId;
                info.pos = unit == null ? col.transform.position : unit.Bounds.center;
                info.rot = col.transform.rotation;
                info.type = comp.Type;
                info.side = comp.Side;
                info.order = comp.Order;
                info.state = comp.State;
                info.rank = comp.Rank;
                info.size = comp.Size;

                index++;
            }

            if (unitList.Count > index) {
                for (var i = index; i < unitList.Count; i++)
                    unitQueue.Enqueue(unitList[i]);
                unitList.RemoveRange(index, unitList.Count - index);
            }

            UnityEngine.Profiling.Profiler.EndSample();

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
        public float size;
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

    public class ComponentCounter<T> where T : UnityEngine.Component
    {
        T value;
        public T GetValue()
        {
            count++;
            return value;
        }

        public int count { get; private set; }

        public ComponentCounter(T comp)
        {
            this.value = comp;
            count = 0;
        }
    }

    public class CounterDictionary<T> where T : Component
    {
        Dictionary<int, ComponentCounter<T>> dic = new Dictionary<int, ComponentCounter<T>>();
        HashSet<int> removeKeys = new HashSet<int>();

        public bool TryGetValue(int id, out T val)
        {
            if (dic.TryGetValue(id, out var counter))
            {
                val = counter.GetValue();
                return true;
            }
            else
            {
                val = null;
                return false;
            }
        }

        public void Add(int id, T val)
        {
            dic.Add(id, new ComponentCounter<T>(val));
        }

        public void RemoveUnderCount(int under)
        {
            removeKeys.Clear();
            foreach (var kvp in dic)
            {
                if (kvp.Value.count < under)
                    removeKeys.Add(kvp.Key);
            }

            foreach (var k in removeKeys)
                dic.Remove(k);
        }
    }
}
