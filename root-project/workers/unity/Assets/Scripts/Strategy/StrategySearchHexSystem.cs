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
    public class StrategySearchHexSystem : SpatialComponentSystem
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
        const int frequencyHex = 15; 

        readonly Dictionary<uint,HexInfo> hexDic = new Dictionary<uint, HexInfo>();

        float hexEdge => HexDictionary.HexEdgeLength;

        protected override void OnCreate()
        {
            base.OnCreate();

            portalGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.HasAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadOnly<Transform>()
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
        }

        private void UpdateHexAccess()
        {
            if (CheckTime(ref interAccess) == false)
                return;

            Entities.With(portalGroup).ForEach((Entity entity,
                                      ref StrategyHexAccessPortal.Component strategy,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                if (status.Side == UnitSide.None)
                    return;

                var hashes = BorderHexList(status.Side);

                var info = strategy.FrontHexInfo;
                CompairList(info.Indexes, hashes);

                strategy.FrontHexInfo = info;
            });
        }

        private void UpdateHexInfo()
        {
            if (CheckTime(ref interHex) == false)
                return;

            Entities.With(hexGroup).ForEach((Entity entity,
                                      ref HexBase.Component hex,
                                      ref Position.Component position,
                                      ref SpatialEntityId entityId) =>
            {
                if (hexDic.ContainsKey(hex.Index) == false)
                    hexDic[hex.Index] = new HexInfo();

                hexDic[hex.Index].Index = hex.Index;
                hexDic[hex.Index].HexId = hex.HexId;
                hexDic[hex.Index].Side = hex.Side;
                hexDic[hex.Index].EntityId = entityId;
            });

            return;
        }

        private HashSet<uint> BorderHexList(UnitSide side)
        {
            HashSet<uint> indexes = null;

            foreach(var kvp in hexDic) {
                if (kvp.Value.Side != side)
                    continue;

                bool isFront = false;
                var index = kvp.Value.Index;
                var ids = HexUtils.GetNeighborHexIndexes(index);
                foreach (var i in ids) {
                    if (hexDic.TryGetValue(i, out var hex))
                        isFront |= hex.Side != side;
                }

                if (!isFront)
                    continue;

                indexes = indexes ?? new HashSet<uint>();
                indexes.Add(index);
            }

            return indexes;
        }

        private void CompairList(List<HexIndex> indexes, HashSet<uint> hashes)
        {
            if (hashes == null)
                return;

            bool isDiff = indexes.Count != hashes.Count;
            foreach (var h in indexes) {
                if (isDiff)
                    break;

                isDiff |= !hashes.Contains(h.Index);
            }

            if (isDiff) {
                indexes.Clear();

                foreach(var h in indexes) {
                    indexes.Add(new HexIndex(hexDic[h.Index].EntityId.EntityId, h.Index));
                }
            }
        }
    }
}
