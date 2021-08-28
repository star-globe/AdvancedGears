using System;
using System.Collections;
using System.Collections.Generic;
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
    public class HexPowerDominationSystem : BaseUnitSearchSystem
    {
        EntityQuerySet dominationQuerySet;
        EntityQuerySet resourceQuerySet;
        const int frequencyPower = 5;
        const int frequencyResource = 1;
        EntityQueryBuilder.F_EDDD<HexPower.Component, HexBase.Component, SpatialEntityId> dominationAction;
        EntityQueryBuilder.F_EDD<HexPowerResource.Component, Position.Component> resourceAction;
        readonly Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>> frontLineDic = new Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>>();

        UnitType[] buildingTypes = null;
        UnitType[] BuildingTypes
        {
            get
            {
                if (buildingTypes == null)
                    buildingTypes = UnitUtils.GetBuildingUnitTypes();

                return buildingTypes;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            dominationQuerySet = new EntityQuerySet(GetEntityQuery(
                                                  ComponentType.ReadOnly<HexPower.Component>(),
                                                  ComponentType.ReadWrite<HexBase.Component>(),
                                                  ComponentType.ReadOnly<HexBase.HasAuthority>(),
                                                  ComponentType.ReadOnly<SpatialEntityId>()
                                                  ), frequencyPower, Time.ElapsedTime);

            resourceQuerySet = new EntityQuerySet(GetEntityQuery(
                                                  ComponentType.ReadOnly<HexPowerResource.Component>(),
                                                  ComponentType.ReadOnly<Position.Component>()
                                                  ), frequencyResource, Time.ElapsedTime);
            dominationAction = DominationQuery;
        }

        private void DominationQuery(Entity entity,
                                    ref HexPower.Component power,
                                    ref HexBase.Component hex,
                                    ref SpatialEntityId entityId)
        {
            UnitSide side = hex.Side;
            UnitState state;

            if (side == UnitSide.None)
            {
                float max = 0;
                foreach (var kvp in power.SidePowers)
                {
                    var p = kvp.Value;
                    if (max >= p)
                        continue;

                    side = kvp.Key;
                    max = p;
                }

                if (max < HexDictionary.HexPowerDomination)
                    return;

                state = UnitState.Alive;
            }
            else
            {
                float min = 0;
                power.SidePowers.TryGetValue(side, out min);

                if (min > HexDictionary.HexPowerMin)
                    return;

                state = UnitState.Dead;
                side = UnitSide.None;
            }

            ForceChangeState(side, state, hex.Index, entityId.EntityId);

            hex.Side = side;
        }

        private void ForceChangeState(UnitSide side, UnitState state, uint index, EntityId entityId)
        {
            var center = HexUtils.GetHexCenter(this.Origin, index, HexDictionary.HexEdgeLength);
            var units = getAllUnits(center, HexDictionary.HexEdgeLength, selfId: null, allowDead: true, BuildingTypes);

            foreach (var u in units)
                this.UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(new ForceStateChange(side, state)), u.id);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref dominationQuerySet.inter) == false)
                return;

            Entities.With(dominationQuerySet.group).ForEach(dominationAction);
        }
    }
}
