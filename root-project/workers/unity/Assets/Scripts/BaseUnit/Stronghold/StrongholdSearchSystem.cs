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
        private EntityQueryBuilder.F_EDDDD<StrongholdSight.Component, BaseUnitStatus.Component, HexFacility.Component, SpatialEntityId> action;
        IntervalChecker inter;

        const int period = 2;

        StrategyHexAccessPortalUpdateSystem portalUpdateSytem = null;
        private Dictionary<UnitSide, FrontHexInfo> FrontHexes => portalUpdateSytem?.FrontHexes;
        private Dictionary<uint, HexIndexPower> HexIndexes => portalUpdateSytem?.HexIndexes;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrongholdSight.Component>(),
                ComponentType.ReadOnly<StrongholdSight.HasAuthority>(),
                ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.HasAuthority>(),
                ComponentType.ReadOnly<HexFacility.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(period);
            action = Query;
            portalUpdateSytem = World.GetExistingSystem<StrategyHexAccessPortalUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach(action);
        }

        private void Query(Entity entity,
                            ref StrongholdSight.Component sight,
                            ref BaseUnitStatus.Component status,
                            ref HexFacility.Component hex,
                            ref SpatialEntityId entityId)
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

            var order = GetTarget(trans.position, hex.HexIndex, this.FrontHexes, this.HexIndexes, status.Side, enemySide, hexes, corners);

            sight.TargetStrongholds = targets;
            sight.FrontLineCorners = corners;
            sight.TargetHexes = hexes;
            status.Order = order;
        }

        readonly List<HexIndex> hexList = new List<HexIndex>();

        private OrderType GetTarget(Vector3 pos, uint index, Dictionary<UnitSide,FrontHexInfo> frontHexes, Dictionary<uint,HexIndexPower> hexIndexes, UnitSide selfSide, UnitSide enemySide, Dictionary<uint,TargetHexInfo> hexes, List<FrontLineInfo> corners)
        {
            var order = OrderType.Idle;
            if (frontHexes.TryGetValue(selfSide, out var frontHexInfo) == false)
                return order;

            hexList.Clear();
            foreach (var i in frontHexInfo.Indexes) {
                if (hexIndexes.ContainsKey(i))
                    hexList.Add(hexIndexes[i].hexIndex);
            }

            FrontHexInfo targetHexInfo;
            if (frontHexes.TryGetValue(UnitSide.None, out targetHexInfo))
                order = GetTargetHex(pos, index, selfSide, hexIndexes, targetHexInfo, hexes);

            if (order != OrderType.Idle) {
                corners.Clear();
                return order;
            }

            hexes.Clear();
            order = GetTargetFrontLine(pos, index, selfSide, hexIndexes, corners);
            return order;
        }

        private OrderType GetTargetFrontLine(in Vector3 pos, uint index, UnitSide selfSide, Dictionary<uint, HexIndexPower> hexIndexes, List<FrontLineInfo> corners)
        {
            corners.Clear();

            int counter = 0;
            var ids = HexUtils.GetNeighborHexIndexes(index);
            foreach (var id in ids)
            {
                if (hexIndexes.ContainsKey(id) == false)
                    continue;
            
                var hex = hexIndexes[id];
                if (hex.Side == UnitSide.None || hex.Side == selfSide)
                    continue;

                counter++;
            }
            if (counter > 0 && hexIndexes.ContainsKey(index)) {
                var hex = hexIndexes[index];
                if (hex.FrontLines.Count > 0) {
                    corners.AddRange(hex.FrontLines);
                    return OrderType.Keep;
                }
            }

            return OrderType.Idle;
        }

        private OrderType GetTargetHex(in Vector3 pos, uint index, UnitSide selfSide, Dictionary<uint, HexIndexPower> hexIndexes, FrontHexInfo targetFront, Dictionary<uint,TargetHexInfo> hexes)
        {
            hexes.Clear();

            int targetCount = 0;
            var indexes = targetFront.Indexes;
            var ids = HexUtils.GetNeighborHexIndexes(index);
            foreach (var id in ids)
            {
                var idx = TargetUtils.FindIndex(indexes, id);
                if (idx < 0)
                    continue;

                if (hexIndexes.ContainsKey(indexes[idx]) == false)
                    continue;

                var hex = hexIndexes[indexes[idx]];
                if (HexUtils.ExistOtherSidePowers(hex.SidePowers, selfSide) == false)
                    continue;

                hexes[hex.Index] = new TargetHexInfo(new HexBaseInfo(hex.Index, UnitSide.None) , 1.0f);
                targetCount++;
            }

            switch (targetCount)
            {
                case 0:
                    return OrderType.Idle;
                case 1:
                    return OrderType.Keep;
                default:
                    return OrderType.Attack;
            }
        }

        private OrderType GetNeighborTargetHexes(uint index,  FrontHexInfo targetFront, Dictionary<uint, TargetHexInfo> hexes)
        {
            bool isTarget = false;
            var neighbors = HexUtils.GetNeighborHexIndexes(index);
            hexes.Clear();
            foreach (var n in neighbors)
            {
                var i = TargetUtils.FindIndex(targetFront.Indexes, n);
                if (i < 0)
                    continue;

                isTarget = true;
                hexes[n] = new TargetHexInfo(new HexBaseInfo(targetFront.Indexes[i], UnitSide.None), 1.0f);
            }

            return isTarget ? OrderType.Attack: OrderType.Keep;
        }

        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, UnitType type, UnitState state, float size, in Vector3 vector, EntityId selfId, Dictionary<EntityId,UnitBaseInfo> targets)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector;
            var range = strategyVector.magnitude;
            var units = getEnemyUnits(side, pos, range, allowDead:true, GetSingleUnitTypes(UnitType.Stronghold));
            if (units != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                units = getAllyUnits(side, newCenter, range, allowDead:true, GetSingleUnitTypes(UnitType.Stronghold));
                if (units != null)
                    units.RemoveAll(u => u.id == selfId);

                if (units != null || units.Count > 0)
                    order = OrderType.Supply;
            }

            targets.Clear();
            if (units != null && units.Count > 0) {
                foreach (var u in units) {
                    targets.Add(u.id, new UnitBaseInfo(u.id, u.pos.ToWorldCoordinates(this.Origin), u.type, u.side, u.state, u.size));
                }
            }
            else {
                targets.Add(selfId, new UnitBaseInfo(selfId, pos.ToWorldCoordinates(this.Origin), type, side, state, size));
                order = OrderType.Keep;
            }

            return order;
        }
        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, UnitType type, UnitState state, float size, in Vector3 vector, EntityId selfId, List<HexIndex> indexes, Dictionary<EntityId, UnitBaseInfo> targets)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector;
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

            var units = getEnemyUnits(side, h_pos, range, allowDead:true, GetSingleUnitTypes(UnitType.Stronghold));
            if (units != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                units = getAllyUnits(side, newCenter, range, allowDead:true, GetSingleUnitTypes(UnitType.Stronghold));
                if (units != null)
                    units.RemoveAll(u => u.id == selfId);
            
                if (units != null || units.Count > 0)
                    order = OrderType.Supply;
            }
            
            targets.Clear();
            if (units != null && units.Count > 0) {
                foreach (var u in units) {
                    targets.Add(u.id, new UnitBaseInfo(u.id, u.pos.ToWorldCoordinates(this.Origin), u.type, u.side, u.state, u.size));
                }
            }
            else {
                targets.Add(selfId, new UnitBaseInfo(selfId, pos.ToWorldCoordinates(this.Origin), type, side, state, size));
                order = OrderType.Keep;
            }

            return order;
        }
    }
}
