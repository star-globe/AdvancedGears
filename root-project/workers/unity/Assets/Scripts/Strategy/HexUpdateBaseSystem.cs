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
    public abstract class HexUpdateBaseSystem : SpatialComponentSystem
    {
        EntityQuery hexGroup;
        IntervalChecker interHex;
        const int updateInter = 5;

        HexBaseSystem hexBaseSystem = null;
        protected Dictionary<uint, HexLocalInfo> HexDic
        {
            get { return hexBaseSystem == null ? null: hexBaseSystem.HexDic; }
        }
        protected float hexEdge => HexDictionary.HexEdgeLength;

        readonly Vector3[] baseCorners = new Vector3[7];
        readonly Vector3[] checkCorners = new Vector3[7];
        readonly int[] cornerIndexes = new int[] { 0, 1, 2, 3, 4, 5 };

        readonly Queue<HexDetails> hexDetailQueue = new Queue<HexDetails>();
        readonly Queue<List<FrontLineInfo>> linesQueue = new Queue<List<FrontLineInfo>>();

        protected override void OnCreate()
        {
            base.OnCreate();

            hexBaseSystem = this.World.GetExistingSystem<HexBaseSystem>();
        }

        protected Dictionary<uint, HexDetails> BorderHexList(UnitSide side, Dictionary<uint, HexDetails> indexes)
        {
            if (this.HexDic == null)
                return null;

            foreach (var kvp in this.HexDic)
            {
                if (kvp.Value.Side != side)
                    continue;

                var index = kvp.Value.Index;

                HexUtils.SetHexCorners(this.Origin, index, baseCorners, HexDictionary.HexEdgeLength);
                var ids = HexUtils.GetNeighborHexIndexes(index);

                List<FrontLineInfo> lines = null;
                foreach (var cornerIndex in cornerIndexes)
                {
                    var right = baseCorners[cornerIndex];
                    var left = baseCorners[cornerIndex + 1];

                    var id = CheckTouch(side, left, right, checkCorners, ids);
                    if (id != null)
                    {
                        if (lines == null) {
                            if (linesQueue.Count > 0)
                                lines = linesQueue.Dequeue();
                            else
                                lines = new List<FrontLineInfo>();
                        }

                        lines.Add(new FrontLineInfo()
                        {
                            LeftCorner = left.ToWorldPosition(this.Origin).ToCoordinates(),
                            RightCorner = right.ToWorldPosition(this.Origin).ToCoordinates()
                        });
                    }
                }

                if (lines == null)
                    continue;

                HexDetails hexDetails = null;
                if (hexDetailQueue.Count > 0)
                    hexDetails = hexDetailQueue.Dequeue();
                else
                    hexDetails = new HexDetails();

                hexDetails.frontLines = lines;
                hexDetails.staminas = kvp.Value.Powers;

                indexes = indexes ?? new Dictionary<uint, HexDetails>();
                indexes.Add(index, hexDetails);
            }

            return indexes;
        }

        uint? CheckTouch(UnitSide side, Vector3 tgtLeft, Vector3 tgtRight, Vector3[] checkCorners, uint[] ids)
        {
            if (this.HexDic == null)
                return null;

            foreach (var id in ids)
            {
                if (this.HexDic.TryGetValue(id, out var hex) == false ||
                    hex.Side == side)
                    continue;

                HexUtils.SetHexCorners(this.Origin, id, checkCorners, HexDictionary.HexEdgeLength);

                if (HexUtils.CheckLine(tgtRight, tgtLeft, checkCorners, HexDictionary.HexEdgeLength / 10000))
                    return id;
            }

            return null;
        }

        protected class HexDetails
        {
            public List<FrontLineInfo> frontLines;
            public Dictionary<UnitSide, float> staminas;
        }

        protected void CompairList(List<uint> indexes, Dictionary<uint, HexDetails> dic)
        {
            if (dic == null)
            {
                indexes.Clear();
                return;
            }

            bool isDiff = indexes.Count != dic.Count;
            foreach (var id in indexes)
            {
                if (isDiff)
                    break;

                isDiff |= !dic.ContainsKey(id);
            }

            if (isDiff)
            {
                indexes.Clear();

                foreach (var kvp in dic)
                {
                    var id = kvp.Key;
                    indexes.Add(id);
                }
            }
        }

        protected void StoreDetailsQueue(Dictionary<uint, HexDetails> indexes)
        {
            if (indexes == null)
                return;

            foreach (var kvp in indexes) {
                var details = kvp.Value;
                details.frontLines.Clear();
                linesQueue.Enqueue(details.frontLines);

                details.frontLines = null;
                details.staminas = null;
                hexDetailQueue.Enqueue(details);
            }

            indexes.Clear();
        }
    }

    public class HexLocalInfo
    {
        public uint Index;
        public int HexId;
        public UnitSide Side;
        public SpatialEntityId EntityId;
        public Dictionary<UnitSide, float> Powers;
        public bool isActive;

        public float CurrentPower
        {
            get
            {
                Powers.TryGetValue(Side, out var current);
                return current;
            }
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class HexBaseSystem : SpatialComponentSystem
    {
        EntityQuerySet hexEntityQuerySet;
        const int updateInter = 5;

        readonly private Dictionary<uint, HexLocalInfo> hexDic = new Dictionary<uint, HexLocalInfo>();
        public Dictionary<uint, HexLocalInfo> HexDic => this.hexDic; 
        protected float hexEdge => HexDictionary.HexEdgeLength;

        protected override void OnCreate()
        {
            base.OnCreate();

            hexEntityQuerySet = new EntityQuerySet(GetEntityQuery(
                                                   ComponentType.ReadOnly<HexBase.Component>(),
                                                   ComponentType.ReadOnly<HexPower.Component>(),
                                                   ComponentType.ReadOnly<Position.Component>(),
                                                   ComponentType.ReadOnly<SpatialEntityId>()
                                                   ), updateInter);
        }

        protected override void OnUpdate()
        {
            UpdateHexInfo();
        }

        private void UpdateHexInfo()
        {
            if (CheckTime(ref hexEntityQuerySet.inter) == false && hexDic.Count > 0)
                return;

            Entities.With(hexEntityQuerySet.group).ForEach((Entity entity,
                                                            ref HexBase.Component hex,
                                                            ref HexPower.Component power,
                                                            ref Position.Component position,
                                                            ref SpatialEntityId entityId) =>
            {
                if (hex.Attribute.IsTargetable() == false)
                    return;

                if (hexDic.ContainsKey(hex.Index) == false)
                    hexDic[hex.Index] = new HexLocalInfo();

                hexDic[hex.Index].Index = hex.Index;
                hexDic[hex.Index].HexId = hex.HexId;
                hexDic[hex.Index].Side = hex.Side;
                hexDic[hex.Index].EntityId = entityId;
                hexDic[hex.Index].Powers = power.SidePowers;
                hexDic[hex.Index].isActive = power.IsActive;
            });
        }
    }
}
