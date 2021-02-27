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
        EntityQuery facilityGroup;
        IntervalChecker interAccess;
        const int frequencyManager = 5;

        protected bool hexChanged { get; private set; } = false;

        protected override void OnCreate()
        {
            base.OnCreate();

            portalGroup = GetEntityQuery(
                ComponentType.ReadWrite<StrategyHexAccessPortal.Component>(),
                ComponentType.ReadOnly<StrategyHexAccessPortal.HasAuthority>(),
                ComponentType.ReadOnly<HexFacility.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            facilityGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexFacility.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interAccess = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyManager);
        }

        protected override void OnUpdate()
        {
            UpdateHexFacility();

            UpdateHexAccess();
        }

        private void UpdateHexFacility()
        {
            if (base.HexDic == null)
                return;

            int changedCount = 0;
            Entities.With(facilityGroup).ForEach((Entity entity,
                                      ref HexFacility.Component facility,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                var hexIndex = facility.HexIndex;
                var side = status.Side;

                if (HexDic.ContainsKey(hexIndex) == false)
                    return;

                var hex = HexDic[hexIndex];
                if (hex.Side == status.Side)
                    return;

                changedCount++;
                hex.Side = status.Side;
                this.UpdateSystem.SendEvent(new HexBase.SideChanged.Event(new SideChangedEvent(side)), hex.EntityId.EntityId);

                Debug.LogFormat("Updated Index:{0} Side:{1}", hexIndex, side);
            });

            hexChanged = changedCount > 0;
        }

        readonly Dictionary<UnitSide,Dictionary<uint, HexDetails>> borderHexListDic = new Dictionary<UnitSide,Dictionary<uint, HexDetails>>();

        private void UpdateHexAccess()
        {
            if (hexChanged == false && CheckTime(ref interAccess) == false)
                return;

            // Check
            CheckBorderHexList();

            Entities.With(portalGroup).ForEach((Entity entity,
                                      ref StrategyHexAccessPortal.Component portal,
                                      ref BaseUnitStatus.Component status,
                                      ref HexFacility.Component facility,
                                      ref SpatialEntityId entityId) =>
            {
                if (status.Side == UnitSide.None)
                    return;

                var fronts = portal.FrontHexes;
                var hexes = portal.HexIndexes;

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
                    CompairList(info.Indexes, indexes);
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
                    hex.SidePowers = info.Powers;
                    hex.Side = info.Side;

                    hexes[index] = hex;
                }

                portal.Index = facility.HexIndex;
                portal.FrontHexes = fronts;
                portal.HexIndexes = hexes;
            });
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
