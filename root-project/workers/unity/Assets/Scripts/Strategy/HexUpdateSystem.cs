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
    public class HexUpdateSystem : HexUpdateBaseSystem
    {
        EntityQuery portalGroup;
        IntervalChecker interAccess;
        const int frequencyManager = 5;

        protected override void OnCreate()
        {
            base.OnCreate();

            portalGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.HasAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interAccess = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyManager);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdateHexAccess();
        }

        private void UpdateHexAccess()
        {
            if (base.hexChanged == false && CheckTime(ref interAccess) == false)
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

        private Dictionary<uint, List<FrontLineInfo>> BorderHexList(UnitSide side)
        {
            Dictionary<uint, List<FrontLineInfo>> indexes = null;

            foreach (var kvp in base.hexDic)
            {
                if (kvp.Value.Side != side)
                    continue;

                var index = kvp.Value.Index;

                var baseCorners = new Vector3[7];
                var checkCorners = new Vector3[7];

                HexUtils.SetHexCorners(this.Origin, index, baseCorners, HexDictionary.HexEdgeLength);
                var ids = HexUtils.GetNeighborHexIndexes(index);

                ///HashSet<int> cornerIndexes = new HashSet<int>() { 0, 1, 2, 3, 4, 5, 6 };
                List<FrontLineInfo> lines = null;
                int[] cornerIndexes = new int[] { 0, 1, 2, 3, 4, 5 };
                foreach (var cornerIndex in cornerIndexes)
                {
                    var right = baseCorners[cornerIndex];
                    var left = baseCorners[cornerIndex + 1];

                    if (CheckTouch(side, left, right, checkCorners, ids)){
                        lines = lines ?? new List<FrontLineInfo>();
                        lines.Add(new FrontLineInfo() { LeftCorner = left.ToWorldPosition(this.Origin).ToCoordinates(),
                                                        RightCorner = right.ToWorldPosition(this.Origin).ToCoordinates()});
                    }
                    //bool isTouched = false;
                    //foreach (var id in ids)
                    //{
                    //    if (base.hexDic.TryGetValue(id, out var hex) == false ||
                    //        hex.Side == side)
                    //        continue;
                    //
                    //    isFront = true;
                    //
                    //    HexUtils.SetHexCorners(this.Origin, id, checkCorners, HexDictionary.HexEdgeLength);
                    //
                    //    if (checkCorners.Any(c => (c - tgt).sqrMagnitude < HexDictionary.HexEdgeLength / 10000))
                    //        isTouched = true;
                    //}
                    //
                    //if (isTouched == false)
                    //    cornerIndexes.Remove(cornerIndex);
                }

                if (lines == null)
                    continue;

                //var list = cornerIndexes.OrderByDescending(i => i)
                //                        .Select(i => baseCorners[i].ToWorldPosition(this.Origin).ToCoordinates()).ToList();
                indexes = indexes ?? new Dictionary<uint, List<FrontLineInfo>>();
                indexes.Add(index, lines);
            }

            return indexes;
        }

        bool CheckTouch(UnitSide side, Vector3 tgtLeft, Vector3 tgtRight, Vector3[] checkCorners, uint[] ids)
        {
            foreach (var id in ids)
            {
                if (base.hexDic.TryGetValue(id, out var hex) == false ||
                    hex.Side == side)
                    continue;

                HexUtils.SetHexCorners(this.Origin, id, checkCorners, HexDictionary.HexEdgeLength);

                if (HexUtils.CheckLine(tgtRight, tgtLeft, checkCorners, HexDictionary.HexEdgeLength / 10000))
                    return true;
            }

            return false;
        }

        private void CompairList(List<HexIndex> indexes, Dictionary<uint, List<FrontLineInfo>> dic)
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
                    indexes.Add(new HexIndex(base.hexDic[id].EntityId.EntityId, id, dic[id]));
                }
            }
        }
    }
}
