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
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class HexUpdateBaseSystem : SpatialComponentSystem
    {
        protected class HexInfo
        {
            public uint Index;
            public int HexId;
            public UnitSide Side;
            public SpatialEntityId EntityId;
            public float TotalPower;
        }

        EntityQuery hexGroup;
        IntervalChecker interHex;
        const int updateInter = 5;

        readonly protected Dictionary<uint, HexInfo> hexDic = new Dictionary<uint, HexInfo>();

        protected float hexEdge => HexDictionary.HexEdgeLength;

        protected override void OnCreate()
        {
            base.OnCreate();

            hexGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<HexPower.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interHex = IntervalCheckerInitializer.InitializedChecker(updateInter);
        }

        protected override void OnUpdate()
        {
            UpdateHexInfo();
        }

        private void UpdateHexInfo()
        {
            if (CheckTime(ref interHex) == false && hexDic.Count > 0)
                return;

            Entities.With(hexGroup).ForEach((Entity entity,
                                      ref HexBase.Component hex,
                                      ref HexPower.Component power,
                                      ref Position.Component position,
                                      ref SpatialEntityId entityId) =>
            {
                if (hex.Attribute.IsTargetable() == false)
                    return;

                if (hexDic.ContainsKey(hex.Index) == false)
                    hexDic[hex.Index] = new HexInfo();

                hexDic[hex.Index].Index = hex.Index;
                hexDic[hex.Index].HexId = hex.HexId;
                hexDic[hex.Index].Side = hex.Side;
                hexDic[hex.Index].EntityId = entityId;
                hexDic[hex.Index].TotalPower = power.StackPower + power.RealizedPower;
            });
        }

        protected Dictionary<uint, List<FrontLineInfo>> BorderHexList(UnitSide side)
        {
            Dictionary<uint, List<FrontLineInfo>> indexes = null;

            foreach (var kvp in this.hexDic)
            {
                if (kvp.Value.Side != side)
                    continue;

                var index = kvp.Value.Index;

                var baseCorners = new Vector3[7];
                var checkCorners = new Vector3[7];

                HexUtils.SetHexCorners(this.Origin, index, baseCorners, HexDictionary.HexEdgeLength);
                var ids = HexUtils.GetNeighborHexIndexes(index);

                List<FrontLineInfo> lines = null;
                int[] cornerIndexes = new int[] { 0, 1, 2, 3, 4, 5 };
                foreach (var cornerIndex in cornerIndexes)
                {
                    var right = baseCorners[cornerIndex];
                    var left = baseCorners[cornerIndex + 1];

                    var id = CheckTouch(side, left, right, checkCorners, ids);
                    if (id != null)
                    {
                        lines = lines ?? new List<FrontLineInfo>();
                        lines.Add(new FrontLineInfo()
                        {
                            LeftCorner = left.ToWorldPosition(this.Origin).ToCoordinates(),
                            RightCorner = right.ToWorldPosition(this.Origin).ToCoordinates()
                        });
                    }
                }

                if (lines == null)
                    continue;

                indexes = indexes ?? new Dictionary<uint, List<FrontLineInfo>>();
                indexes.Add(index, lines);
            }

            return indexes;
        }

        uint? CheckTouch(UnitSide side, Vector3 tgtLeft, Vector3 tgtRight, Vector3[] checkCorners, uint[] ids)
        {
            foreach (var id in ids)
            {
                if (this.hexDic.TryGetValue(id, out var hex) == false ||
                    hex.Side == side)
                    continue;

                HexUtils.SetHexCorners(this.Origin, id, checkCorners, HexDictionary.HexEdgeLength);

                if (HexUtils.CheckLine(tgtRight, tgtLeft, checkCorners, HexDictionary.HexEdgeLength / 10000))
                    return id;
            }

            return null;
        }

        protected void CompairList(List<HexIndex> indexes, Dictionary<uint, List<FrontLineInfo>> dic)
        {
            if (dic == null)
            {
                indexes.Clear();
                return;
            }

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
                    indexes.Add(new HexIndex(this.hexDic[id].EntityId.EntityId, id, dic[id]));
                }
            }
        }
    }
}
