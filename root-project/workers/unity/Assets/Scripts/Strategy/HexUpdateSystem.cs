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
    public class HexUpdateSystem : SpatialComponentSystem
    {
        class HexInfo
        {
            public uint Index;
            public int HexId;
            public UnitSide Side;
            public SpatialEntityId EntityId;
        }


        EntityQuery portalGroup;
        EntityQuery hexGroup;
        IntervalChecker interAccess;
        IntervalChecker interHex;
        const int frequencyManager = 5;
        const int frequencyHex = 5;

        bool hexChanged = false;
        readonly Dictionary<uint, HexInfo> hexDic = new Dictionary<uint, HexInfo>();

        float hexEdge => HexDictionary.HexEdgeLength;

        protected override void OnCreate()
        {
            base.OnCreate();

            portalGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.HasAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            hexGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interAccess = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyManager);
            interHex = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyHex);
        }

        protected override void OnUpdate()
        {
            UpdateHexInfo();

            UpdateHexAccess();

            hexChanged = false;
        }

        private void UpdateHexAccess()
        {
            if (hexChanged = false && CheckTime(ref interAccess) == false)
                return;

            Entities.With(portalGroup).ForEach((Entity entity,
                                      ref StrategyHexAccessPortal.Component strategy,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                if (status.Side == UnitSide.None)
                    return;

                var hexes = strategy.FrontHexes;

                foreach (var side in HexUtils.AllSides)
                {
                    var dic = BorderHexList(side);

                    if (hexes.ContainsKey(side) == false)
                        hexes[side] = new FrontHexInfo { Indexes = new List<HexIndex>() };

                    var info = hexes[side];
                    CompairList(info.Indexes, dic);
                    hexes[side] = info;
                }

                strategy.FrontHexes = hexes;
            });
        }

        private void UpdateHexInfo()
        {
            if (hexChanged == false && CheckTime(ref interHex) == false)
                return;

            Entities.With(hexGroup).ForEach((Entity entity,
                                      ref HexBase.Component hex,
                                      ref Position.Component position,
                                      ref SpatialEntityId entityId) =>
            {
                if (hex.Attribute.IsDominatable() == false)
                    return;

                if (hexDic.ContainsKey(hex.Index) == false)
                    hexDic[hex.Index] = new HexInfo();

                hexDic[hex.Index].Index = hex.Index;
                hexDic[hex.Index].HexId = hex.HexId;
                hexDic[hex.Index].Side = hex.Side;
                hexDic[hex.Index].EntityId = entityId;
            });
        }

        private Dictionary<uint, List<Coordinates>> BorderHexList(UnitSide side)
        {
            Dictionary<uint, List<Coordinates>> indexes = null;

            foreach (var kvp in hexDic)
            {
                if (kvp.Value.Side != side)
                    continue;

                bool isFront = false;
                var index = kvp.Value.Index;

                var baseCorners = new Vector3[7];
                var checkCorners = new Vector3[7];

                HexUtils.SetHexCorners(this.Origin, index, baseCorners, HexDictionary.HexEdgeLength);
                var ids = HexUtils.GetNeighborHexIndexes(index);

                HashSet<int> cornerIndexes = new HashSet<int>() { 0, 1, 2, 3, 4, 5 };
                foreach (var cornerIndex in cornerIndexes.ToArray())
                {
                    var tgt = baseCorners[cornerIndex];

                    bool isTouched = false;
                    foreach (var id in ids)
                    {
                        if (hexDic.TryGetValue(id, out var hex) == false ||
                            hex.Side == side)
                            continue;

                        isFront = true;

                        HexUtils.SetHexCorners(this.Origin, id, checkCorners, HexDictionary.HexEdgeLength);

                        if (checkCorners.Any(c => (c - tgt).sqrMagnitude < HexDictionary.HexEdgeLength / 10000))
                            isTouched = true;
                    }

                    if (isTouched == false)
                        cornerIndexes.Remove(cornerIndex);
                }

                if (!isFront)
                    continue;

                var list = cornerIndexes.OrderByDescending(i => i)
                                        .Select(i => baseCorners[i].ToWorldPosition(this.Origin).ToCoordinates()).ToList();
                indexes = indexes ?? new Dictionary<uint, List<Coordinates>>();
                indexes.Add(index, list);
            }

            return indexes;
        }

        private void CompairList(List<HexIndex> indexes, Dictionary<uint, List<Coordinates>> dic)
        {
            if (dic == null)
                return;

            bool isDiff = indexes.Count != dic.Count;
            foreach (var h in indexes)
            {
                if (isDiff)
                    break;

                isDiff |= !dic.ContainsKey(h.Index);
            }

            if (isDiff)
            {
                indexes.Clear();

                foreach (var kvp in dic)
                {
                    var id = kvp.Key;
                    indexes.Add(new HexIndex(hexDic[id].EntityId.EntityId, id, dic[id]));
                }
            }
        }

        public void HexChanged(uint index, UnitSide side)
        {
            if (hexDic.ContainsKey(index) && hexDic[index].Side != side)
                hexChanged = true;
        }
    }
}
