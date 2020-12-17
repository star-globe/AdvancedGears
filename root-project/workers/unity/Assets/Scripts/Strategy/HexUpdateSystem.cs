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

            base.OnUpdate();

            UpdateHexAccess();
        }

        private void UpdateHexFacility()
        {
            int changedCount = 0;
            Entities.With(facilityGroup).ForEach((Entity entity,
                                      ref HexFacility.Component facility,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                var hexIndex = facility.HexIndex;
                var side = status.Side;

                if (hexDic.ContainsKey(hexIndex) == false)
                    return;

                var hex = hexDic[hexIndex];
                if (hex.Side == status.Side)
                    return;

                changedCount++;
                hex.Side = status.Side;
                this.UpdateSystem.SendEvent(new HexBase.SideChanged.Event(new SideChangedEvent(side)), hex.EntityId.EntityId);

                Debug.LogFormat("Updated Index:{0} Side:{1}", hexIndex, side);
            });

            hexChanged = changedCount > 0;
        }

        private void UpdateHexAccess()
        {
            if (hexChanged == false && CheckTime(ref interAccess) == false)
                return;

            Entities.With(portalGroup).ForEach((Entity entity,
                                      ref StrategyHexAccessPortal.Component strategy,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                if (status.Side == UnitSide.None)
                    return;

                var fronts = strategy.FrontHexes;
                var hexes = strategy.HexIndexes;

                hexes.Clear();
                foreach (var side in HexUtils.AllSides)
                {
                    var dic = BorderHexList(side);

                    if (fronts.ContainsKey(side) == false)
                        fronts[side] = new FrontHexInfo { Indexes = new List<uint>() };

                    var info = fronts[side];
                    CompairList(info.Indexes, dic);
                    fronts[side] = info;
                }

                hexes.Clear();
                foreach (var kvp in this.hexDic) {
                    var hex = new HexIndex();
                    var info = kvp.Value;
                    hex.EntityId = info.EntityId.EntityId;
                    hexes.Add(kvp.Key, hex);
                }

                strategy.FrontHexes = fronts;
            });
        }
    }
}
