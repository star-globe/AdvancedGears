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
    public class StrategyHexAccessPortalUpdateSystem : SpatialComponentSystem
    {
        EntityQuerySet portalQuerySet;
        private EntityQueryBuilder.F_ED<StrategyHexAccessPortal.Component> portalAction;
        const float frequency = 1.0f; 
        private Dictionary<uint, HexIndexPower> hexIndexes;
        public Dictionary<uint, HexIndexPower> HexIndexes => hexIndexes;
        private Dictionary<UnitSide, FrontHexInfo> frontHexes;
        public Dictionary<UnitSide, FrontHexInfo> FrontHexes => frontHexes;

        HexBaseSystem hexBaseSystem = null;
        HexBaseSystem HexBaseSystem
        {
            get
            {
                hexBaseSystem = hexBaseSystem ?? this.World.GetExistingSystem<HexBaseSystem>();
                return hexBaseSystem;
            }
        }
        protected Dictionary<uint, HexLocalInfo> HexDic
        {
            get { return this.HexBaseSystem ?.HexDic; }
        }

        bool isUpdated = false;
        protected override void OnCreate()
        {
            base.OnCreate();

            portalQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadOnly<StrategyHexAccessPortal.Component>()
                                                ), frequency);

            portalAction = PortalQuery;
            this.hexIndexes = new Dictionary<uint, HexIndexPower>();
            this.frontHexes = new Dictionary<UnitSide, FrontHexInfo>();
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref portalQuerySet.inter) == false)
                return;

            isUpdated = false;
            Entities.With(portalQuerySet.group).ForEach(portalAction);
        }

        private void PortalQuery(Unity.Entities.Entity entity,
                                 ref StrategyHexAccessPortal.Component portal)
        {
            if (isUpdated)
                return;

            var indexes = portal.HexIndexes;
            var hexes = portal.FrontHexes;

            if (indexes.Count == 0 && hexes.Count == 0)
                return;

            this.hexIndexes.Clear();
            foreach (var i in indexes) {
                HexLocalInfo local = null;
                this.HexDic?.TryGetValue(i.Key, out local);
                this.hexIndexes.Add(i.Key, new HexIndexPower(i.Value, local));
            }

            this.frontHexes.Clear();
            foreach (var h in hexes)
                this.frontHexes.Add(h.Key, h.Value);

            isUpdated = true;
        }
    }

    public struct HexIndexPower
    {
        public HexIndex hexIndex { get; private set; }
        public HexAttribute attribute { get; private set; }
        public Dictionary<UnitSide, float> SidePowers { get; private set; }
        public uint Index => hexIndex.Index;
        public UnitSide Side => hexIndex.Side;
        public List<FrontLineInfo> FrontLines => hexIndex.FrontLines;

        public HexIndexPower(HexIndex index, HexLocalInfo local)
        {
            this.hexIndex = index;
            this.attribute = local.Attribute;
            SidePowers = local == null ? new Dictionary<UnitSide, float>(): local.Powers;
        }
    }
}
