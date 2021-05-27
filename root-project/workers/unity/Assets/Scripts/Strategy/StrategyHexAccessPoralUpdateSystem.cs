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
        private Dictionary<uint, HexIndex> hexIndexes;
        public Dictionary<uint, HexIndex> HexIndexes => hexIndexes;
        private Dictionary<UnitSide, FrontHexInfo> frontHexes;
        public Dictionary<UnitSide, FrontHexInfo> FrontHexes => frontHexes;

        bool isUpdated = false;
        protected override void OnCreate()
        {
            base.OnCreate();

            portalQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadOnly<StrategyHexAccessPortal.Component>()
                                                ), frequency);

            portalAction = PortalQuery;
            this.hexIndexes = new Dictionary<uint, HexIndex>();
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

            indexes = portal.HexIndexes;
            hexes = portal.FrontHexes;

            this.hexIndexes.Clear();
            foreach (var i in indexes)
                this.hexIndexes.Add(i.Key, i.Value);

            this.frontHexes.Clear();
            foreach (var h in hexes)
                this.frontHexes.Add(h.Key, h.Value);

            isUpdated = true;
        }
    }
}
