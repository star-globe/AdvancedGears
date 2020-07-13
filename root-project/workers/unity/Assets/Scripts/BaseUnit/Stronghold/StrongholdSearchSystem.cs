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
                var vector = sight.StrategyVector.Vector.ToUnityVector();
                var corners = sight.FrontLineCorners;
                var hexes = sight.TargetHexes;
                var order = OrderType.Idle;
                if (portal.FrontHexes.TryGetValue(status.Side, out var frontHexInfo)) {
                    order = GetTargetFrontLine(trans.position, frontHexInfo.Indexes, corners);
                    //GetTargetStronghold(trans.position, status.Side, vector, entityId.EntityId, indexes, targets);
                }

                sight.TargetStrongholds = targets;
                sight.FrontLineCorners = corners;
                sight.TargetHexes = hexes;
                status.Order = order;
            });
        }

        private OrderType GetTargetFrontLine(in Vector3 pos, List<HexIndex> indexes, List<FrontLineInfo> corners)
        {
            HexIndex? targetIndex = null;
            float length = float.MaxValue;
            foreach (var hex in indexes) {
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
            else {
                return OrderType.Idle;
            }
        }

        private OrderType GetTargetHex(in Vector3 pos, List<HexIndex> indexes, Dictionary<UnitSide,FrontHexInfo> fronts, Dictionary<EntityId,TargetHexInfo> hexes)
        {
            HexIndex? targetIndex = null;
            float length = float.MaxValue;
            foreach (var hex in indexes) {
                var h_pos = GetHexCenter(hex.Index);
                var l = (pos - h_pos).sqrMagnitude;
                if (l >= length)
                    continue;

                length = l;
                targetIndex = hex;
            }

            if (targetIndex.HasValue) {
                return GetNeighborTargetHexes(UnitSide.None, targetIndex.Value.Index, fronts, hexes);
            }
            else {
                return OrderType.Idle;
            }
        }

        private OrderType GetNeighborTargetHexes(UnitSide side, uint index, Dictionary<UnitSide, FrontHexInfo> fronts, Dictionary<EntityId, TargetHexInfo> hexes)
        {
            bool isTarget = false;
            if (fronts.TryGetValue(side, out var info))
            {
                var neighbors = HexUtils.GetNeighborHexIndexes(index);
                foreach (var n in neighbors)
                {
                    var i = info.Indexes.FindIndex(h => h.Index == n);
                    if (i < 0)
                        continue;

                    isTarget = true;
                    var hx = info.Indexes[i];
                    hexes[hx.EntityId] = new TargetHexInfo(true, hx.Index, UnitSide.None);
                }
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
