using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class StrongholdSearchSystem : BaseSearchSystem
    {
        private EntityQuery group;
        private EntityQuery hexGroup;
        IntervalChecker inter;

        const int period = 2;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrongholdSight.Component>(),
                ComponentType.ReadOnly<StrongholdSight.HasAuthority>(),
                ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.HasAuthority>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref StrongholdSight.Component sight,
                                          ref BaseUnitStatus.Component status,
                                          ref StrategyHexAccessPortal.Component portal,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var inter = sight.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                sight.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);

                var targets = sight.TargetStrongholds;
                var enemySide = sight.StrategyVector.Side;
                var vector = sight.StrategyVector.Vector.ToUnityVector();
                var corners = sight.FrontLineCorners;
                var hexes = sight.TargetHexes;

                var order = GetTarget(trans.position, portal.FrontHexes, status.Side, enemySide, hexes, corners);

                sight.TargetStrongholds = targets;
                sight.FrontLineCorners = corners;
                sight.TargetHexes = hexes;
                status.Order = order;
            });
        }

        private OrderType GetTarget(Vector3 pos, Dictionary<UnitSide,FrontHexInfo> frontHexes, UnitSide selfSide, UnitSide enemySide, Dictionary<uint,TargetHexInfo> hexes, List<FrontLineInfo> corners)
        {
            var order = OrderType.Idle;
            if (frontHexes.TryGetValue(selfSide, out var frontHexInfo) == false)
                return order;
            
            FrontHexInfo targetHexInfo;
            if (frontHexes.TryGetValue(UnitSide.None, out targetHexInfo))
                order = GetTargetHex(pos, selfSide, frontHexInfo.Indexes, targetHexInfo, hexes);
            
            if (order != OrderType.Idle)
                return order;

#if false
            if (enemySide != UnitSide.None && frontHexes.TryGetValue(enemySide, out targetHexInfo))
                order = GetTargetHex(pos, selfSide, frontHexInfo.Indexes, targetHexInfo, hexes);

            if (order != OrderType.Idle)
                return order;
#endif

            order = GetTargetFrontLine(pos, selfSide, frontHexInfo.Indexes, corners);
            return order;
        }

        const float sumLimit = 1.0f / 10000;
        private OrderType GetTargetFrontLine(in Vector3 pos, UnitSide selfSide, List<HexIndex> indexes, List<FrontLineInfo> corners)
        {
            HexIndex? targetIndex = null;
            float length = float.MaxValue;
            foreach (var hex in indexes) {
                var otherPower = hex.OtherSideSum(selfSide);
                if (otherPower < sumLimit)
                    continue;

                var h_pos = GetHexCenter(hex.Index);
                var l = (pos - h_pos).sqrMagnitude;
                if (l >= length)
                    continue;

                length = l;
                targetIndex = hex;
            }

            if (targetIndex.HasValue) {
                corners.Clear();
                corners.AddRange(targetIndex.Value.FrontLines);
                return OrderType.Keep;
            }

            return OrderType.Idle;
        }

        private OrderType GetTargetHex(in Vector3 pos, UnitSide selfSide, List<HexIndex> indexes, FrontHexInfo targetFront, Dictionary<uint,TargetHexInfo> hexes)
        {
            HexIndex? targetIndex = null;
            float length = float.MaxValue;
            foreach (var hex in indexes) {
                var otherPower = hex.OtherSideSum(selfSide);
                if (otherPower < sumLimit)
                    continue;

                var h_pos = GetHexCenter(hex.Index);
                var l = (pos - h_pos).sqrMagnitude;
                if (l >= length)
                    continue;

                length = l;
                targetIndex = hex;
            }

            if (targetIndex.HasValue)
                return GetNeighborTargetHexes(targetIndex.Value.Index, targetFront, hexes);

            return OrderType.Idle;
        }

        private OrderType GetNeighborTargetHexes(uint index,  FrontHexInfo targetFront, Dictionary<uint, TargetHexInfo> hexes)
        {
            bool isTarget = false;
            var neighbors = HexUtils.GetNeighborHexIndexes(index);
            hexes.Clear();
            foreach (var n in neighbors)
            {
                var i = targetFront.Indexes.FindIndex(h => h.Index == n);
                if (i < 0)
                    continue;
                isTarget = true;
                var hx = targetFront.Indexes[i];
                hexes[n] = new TargetHexInfo(hx.Index, UnitSide.None);
            }

            return isTarget ? OrderType.Attack: OrderType.Keep;
        }

        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, in Vector3 vector, EntityId selfId, Dictionary<EntityId,TargetStrongholdInfo> targets)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector;// * RangeDictionary.StrategyRangeRate;
            var range = strategyVector.magnitude;
            var units = getEnemyUnits(side, pos, range, allowDead:true, UnitType.Stronghold);
            if (units != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                units = getAllyUnits(side, newCenter, range, allowDead:true, UnitType.Stronghold);
                if (units != null)
                    units.RemoveAll(u => u.id == selfId);

                if (units != null || units.Count > 0)
                    order = OrderType.Supply;
            }

            targets.Clear();
            if (units != null && units.Count > 0) {
                foreach (var u in units) {
                    targets.Add(u.id, new TargetStrongholdInfo(u.id, u.side, u.pos.ToWorldPosition(this.Origin).ToCoordinates()));
                }
            }
            else {
                targets.Add(selfId, new TargetStrongholdInfo(selfId, side, pos.ToWorldPosition(this.Origin).ToCoordinates()));
                order = OrderType.Keep;
            }

            //Debug.LogFormat("Side:{0} Order:{1} StrategyVector:{2}", side, order, strategyVector);

            return order;
        }
        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, in Vector3 vector, EntityId selfId, List<HexIndex> indexes, Dictionary<EntityId,TargetStrongholdInfo> targets)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector;// * RangeDictionary.StrategyRangeRate;
            var range = strategyVector.magnitude;
            EntityId? target = null;
            Vector3 h_pos = this.Origin;
            float length = float.MaxValue;
            foreach (var hex in indexes) {
                h_pos = GetHexCenter(hex.Index);
                var l = (pos - h_pos).sqrMagnitude;
                if (l >= length)
                    continue;

                length = l;
                target = hex.EntityId;
            }

            //targets.Clear();
            //if (target != null) {
            //    targets.Add(target.Value, new TargetStrongholdInfo(target.Value, side, h_pos.ToWorldPosition(this.Origin).ToCoordinates()));
            //    order = OrderType.Guard;
            //}
            //else {
            //    order = OrderType.Idle;
            //}

            var units = getEnemyUnits(side, h_pos, range, allowDead:true, UnitType.Stronghold);
            if (units != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                units = getAllyUnits(side, newCenter, range, allowDead:true, UnitType.Stronghold);
                if (units != null)
                    units.RemoveAll(u => u.id == selfId);
            
                if (units != null || units.Count > 0)
                    order = OrderType.Supply;
            }
            
            targets.Clear();
            if (units != null && units.Count > 0) {
                foreach (var u in units) {
                    targets.Add(u.id, new TargetStrongholdInfo(u.id, u.side, u.pos.ToWorldPosition(this.Origin).ToCoordinates()));
                }
            }
            else {
                targets.Add(selfId, new TargetStrongholdInfo(selfId, side, pos.ToWorldPosition(this.Origin).ToCoordinates()));
                order = OrderType.Keep;
            }

            //Debug.LogFormat("Side:{0} Order:{1} StrategyVector:{2}", side, order, strategyVector);

            return order;
        }
    }
}
