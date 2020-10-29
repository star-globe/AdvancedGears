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
        }

        EntityQuery hexGroup;
        EntityQuery facilityGroup;
        IntervalChecker interHex;
        const int updateInter = 5;

        readonly protected Dictionary<uint, HexInfo> hexDic = new Dictionary<uint, HexInfo>();

        protected bool hexChanged { get; private set; } = false;
        protected float hexEdge => HexDictionary.HexEdgeLength;

        protected override void OnCreate()
        {
            base.OnCreate();

            hexGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            facilityGroup = GetEntityQuery(
                ComponentType.ReadOnly<HexFacility.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interHex = IntervalCheckerInitializer.InitializedChecker(updateInter);
        }

        protected override void OnUpdate()
        {
            UpdateHexFacility();

            UpdateHexInfo();
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

        private void UpdateHexInfo()
        {
            if (CheckTime(ref interHex) == false && hexDic.Count > 0)
                return;

            Entities.With(hexGroup).ForEach((Entity entity,
                                      ref HexBase.Component hex,
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
            });
        }
    }
}
