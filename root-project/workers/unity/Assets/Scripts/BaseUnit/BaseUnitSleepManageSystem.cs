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
    [Obsolete]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class BaseUnitSleepManageSystem : BaseSearchSystem
    {
        EntityQuerySet unitQuerySet;
        private EntityQueryBuilder.F_EDD<BaseUnitStatus.Component, Position.Component> unitAction;
        EntityQuerySet portalQuerySet;
        private EntityQueryBuilder.F_ED<StrategyHexAccessPortal.Component> portalAction;
        const float frequency =1.0f; 
        private Dictionary<uint, HexIndex> hexIndexes;

        protected override void OnCreate()
        {
            base.OnCreate();

            unitQuerySet = new EntityQuerySet(GetEntityQuery(
                                              ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                                              ComponentType.ReadOnly<BaseUnitStatus.HasAuthority>(),
                                              ComponentType.ReadOnly<Position.Component>(),
                                              ComponentType.ReadOnly<Rigidbody>()
                                              ), frequency);
            unitAction = UnitQuery;

            portalQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadOnly<StrategyHexAccessPortal.Component>()
                                                ), frequency);

            portalAction = PortalQuery;
        }

        protected override void OnUpdate()
        {
            GatherPortalData();
        }

        void GatherPortalData()
        {
            if (CheckTime(ref portalQuerySet.inter) == false)
                return;

            Entities.With(portalQuerySet.group).ForEach(portalAction);
        }

        private void PortalQuery(Unity.Entities.Entity entity,
                                 ref StrategyHexAccessPortal.Component portal)
        {
            hexIndexes = portal.HexIndexes;
        }

        private void UpdateBaseUnitSleep()
        {
            if (CheckTime(ref unitQuerySet.inter) == false)
                return;

            Entities.With(unitQuerySet.group).ForEach(unitAction);
        }
        
        private void UnitQuery(Entity entity,
                                ref BaseUnitStatus.Component status,
                                ref Position.Component position)
        {
            bool isActive = false;
            var pos = position.Coords.ToUnityVector() + this.Origin;
            foreach (var kvp in hexIndexes) {
                if (HexUtils.IsInsideHex(this.Origin, kvp.Key, pos) == false)
                    continue;

                isActive = kvp.Value.IsActive;
                break;
            }

            if (isActive) {
                if (status.State == UnitState.Sleep)
                    status.State = UnitState.Alive;
            }
            else
            {
                if (status.State == UnitState.Alive)
                    status.State = UnitState.Sleep;
            }
        }
    }
}
