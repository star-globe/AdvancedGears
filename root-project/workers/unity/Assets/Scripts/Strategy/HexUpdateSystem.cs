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
        EntityQueryBuilder.F_ED<StrategyHexAccessPortal.Component> portalQuery;
        EntityQuery facilityGroup;
        //EntityQueryBuilder.F_EDDD<HexFacility.Component, BaseUnitStatus.Component, SpatialEntityId> facilityQuery;
        IntervalChecker interAccess;
        const int frequencyManager = 1;

        int changedCount = 0;
        protected bool hexChanged { get { return changedCount > 0; } }

        protected override void OnCreate()
        {
            base.OnCreate();

            portalGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.HasAuthority>()
            );

            facilityGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexFacility.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interAccess = IntervalCheckerInitializer.InitializedChecker(frequencyManager);

            portalQuery = PortalQuery;
            //facilityQuery = FacilityQuery;
        }

        protected override void OnUpdate()
        {
            //UpdateHexFacility();
            CheckHexSideCheange();

            UpdateHexAccess();
        }

        //private void UpdateHexFacility()
        //{
        //    if (base.HexDic == null)
        //        return;
        //
        //    changedCount = 0;
        //    Entities.With(facilityGroup).ForEach(facilityQuery);
        //}

        //private void FacilityQuery(Entity entity,
        //                            ref HexFacility.Component facility,
        //                            ref BaseUnitStatus.Component status,
        //                            ref SpatialEntityId entityId)
        //{
        //    var hexIndex = facility.HexIndex;
        //    var side = status.Side;
        //
        //    if (HexDic.ContainsKey(hexIndex) == false)
        //        return;
        //
        //    var hex = HexDic[hexIndex];
        //    if (hex.Side == status.Side)
        //        return;
        //
        //    changedCount++;
        //    //hex.Side = status.Side;
        //    //this.UpdateSystem.SendEvent(new HexBase.SideChanged.Event(new SideChangedEvent(side)), hex.EntityId.EntityId);
        //
        //    //Debug.LogFormat("Updated Index:{0} Side:{1}", hexIndex, side);
        //}

        private void CheckHexSideCheange()
        {
            var changedEvents = this.UpdateSystem.GetEventsReceived<HexBase.SideChanged.Event>();
            changedCount = changedEvents.Count;
        }

        readonly Dictionary<UnitSide,Dictionary<uint, HexDetails>> borderHexListDic = new Dictionary<UnitSide,Dictionary<uint, HexDetails>>();

        private void UpdateHexAccess()
        {
            if (hexChanged == false && CheckTime(ref interAccess) == false)
                return;

            // Check
            CheckBorderHexList();

            Entities.With(portalGroup).ForEach(portalQuery);
        }

        readonly Dictionary<UnitSide, FrontHexInfo> fronts = new Dictionary<UnitSide, FrontHexInfo>();
        readonly Dictionary<uint, HexIndex> hexes = new Dictionary<uint, HexIndex>();

        private void PortalQuery(Entity entity,
                                 ref StrategyHexAccessPortal.Component portal)
        {
            var f = portal.FrontHexes;
            var h = portal.HexIndexes;

            DeepCopy(f, h);

            foreach (var side in HexUtils.AllSides)
            {
                Dictionary<uint, HexDetails> indexes = null;
                if (borderHexListDic.TryGetValue(side, out indexes) == false) {
                    if (fronts.ContainsKey(side))
                        fronts.Remove(side);
                    continue;
                }

                if (fronts.ContainsKey(side) == false)
                    fronts[side] = new FrontHexInfo { Indexes = new List<uint>() };

                var info = fronts[side];
                if (CompairList(info.Indexes, indexes))
                    fronts[side] = info;
            }

            foreach (var kvp in this.HexDic) {
                var index = kvp.Key;
                if (hexes.ContainsKey(index) == false)
                    hexes[index] = new HexIndex();

                var info = kvp.Value;
                var hex = hexes[index];
                hex.Index = info.Index;
                hex.EntityId = info.EntityId.EntityId;
                List<FrontLineInfo> frontLines = hex.FrontLines;

                if (borderHexListDic.TryGetValue(info.Side, out var borderList) &&
                    borderList != null && 
                    borderList.TryGetValue(index, out var detail))
                    frontLines = detail.frontLines;
                else
                    frontLines = frontLines ?? new List<FrontLineInfo>();

                hex.FrontLines = frontLines;
                hex.IsActive = info.isActive;
                hex.Side = info.Side;

                hexes[index] = hex;
            }

            if (CheckFrontsDiff(f))
                portal.FrontHexes = fronts;

            if (CheckHexesDiff(h))
                portal.HexIndexes = hexes;
        }

        private void DeepCopy(Dictionary<UnitSide, FrontHexInfo> f, Dictionary<uint, HexIndex> h)
        {
            this.fronts.Clear();
            foreach (var kvp in f)
                this.fronts[kvp.Key] = kvp.Value;

            this.hexes.Clear();
            foreach (var kvp in h)
                this.hexes[kvp.Key] = kvp.Value;
        }

        private bool CheckFrontsDiff(Dictionary<UnitSide, FrontHexInfo> f)
        {
            if (f.Count != this.fronts.Count)
                return true;

            foreach (var kvp in f) {
                var key = kvp.Key;
                if (this.fronts.ContainsKey(key) == false)
                    return true;

                var ids = this.fronts[key].Indexes;
                var values = kvp.Value.Indexes;

                if (ids.Count != values.Count)
                    return true;

                for (var i = 0; i < ids.Count; i++)
                    if (ids[i] != values[i])
                        return true;
            }

            return false;
        }

        private bool CheckHexesDiff(Dictionary<uint, HexIndex> h)
        {
            if (h.Count != this.hexes.Count)
                return true;

            foreach (var kvp in h)
            {
                var key = kvp.Key;
                if (this.hexes.ContainsKey(key) == false)
                    return true;

                var hex = this.hexes[key];
                var val = kvp.Value;

                if (hex.Index != val.Index)
                    return true;

                if (hex.IsActive != val.IsActive)
                    return true;

                if (hex.Side != val.Side)
                    return true;

                if (hex.EntityId != val.EntityId)
                    return true;

                if (hex.FrontLines.Count != val.FrontLines.Count)
                    return true;

                for (var i = 0; i < hex.FrontLines.Count; i++) {
                    var la = hex.FrontLines[i];
                    var lb = val.FrontLines[i];

                    if (la.CornerEquals(lb) == false)
                        return true;
                }
            }

            return false;
        }

        private void CheckBorderHexList()
        {
            foreach (var side in HexUtils.AllSides)
            {
                Dictionary<uint, HexDetails> indexes = null;
                if (borderHexListDic.TryGetValue(side, out indexes))
                    StoreDetailsQueue(indexes);

                borderHexListDic[side] = BorderHexList(side, indexes);
            }
        }
    }
}
