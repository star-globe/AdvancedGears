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
    public class HexPowerUpdateSystem : HexUpdateBaseSystem
    {
        EntityQuerySet hexpowerQuerySet;
        EntityQuerySet resourceQuerySet;
        const int frequencyPower = 5;
        const int frequencyResource = 1;

        readonly Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>> frontLineDic = new Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>>();

        protected override void OnCreate()
        {
            base.OnCreate();

            hexpowerQuerySet = new EntityQuerySet(GetEntityQuery(
                                                  ComponentType.ReadWrite<HexPower.Component>(),
                                                  ComponentType.ReadOnly<HexPower.HasAuthority>(),
                                                  ComponentType.ReadOnly<HexBase.Component>(),
                                                  ComponentType.ReadOnly<SpatialEntityId>()
                                                  ), frequencyPower, Time.ElapsedTime);

            resourceQuerySet = new EntityQuerySet(GetEntityQuery(
                                                  ComponentType.ReadOnly<HexPowerResource.Component>(),
                                                  ComponentType.ReadOnly<Position.Component>()
                                                  ), frequencyResource, Time.ElapsedTime);
        }

        readonly Dictionary<uint, float> resourceDictionary = new Dictionary<uint, float>();

        protected override void OnUpdate()
        {
            UpdateHexResource();

            UpdateHexPower();
        }

        const float flowValue = 1.0f;

        private void UpdateHexPower()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref hexpowerQuerySet.inter) == false)
                return;

            var deltaTime = hexpowerQuerySet.GetDelta(Time.ElapsedTime);

            Entities.With(hexpowerQuerySet.group).ForEach((Entity entity,
                                                           ref HexPower.Component power,
                                                           ref HexBase.Component hex,
                                                           ref SpatialEntityId entityId) =>
            {
                if (hex.Side == UnitSide.None)
                    return;

                power.SidePowers.TryGetValue(hex.Side, out var current);

                resourceDictionary.TryGetValue(hex.Index, out var resourceValue);

                power.SidePowers[hex.Side] = current + (float)(resourceValue * deltaTime);

                var flow = (float)(flowValue * deltaTime);

                int count = -1;
                var ids = HexUtils.GetNeighborHexIndexes(hex.Index);
                foreach (var id in ids)
                {
                    count++;

                    if (base.HexDic.ContainsKey(id) == false)
                        continue;

                    var h = base.HexDic[id];
                    bool isFlow = false;
                    if (h.Side == hex.Side)
                    {
                        if (current > h.CurrentPower && current >= flow)
                        {
                            isFlow = true;
                        }
                    }
                    else if (h.Side == UnitSide.None)
                    {
                        isFlow = true;
                    }

                    if (isFlow)
                    {
                        float val = flow;
                        if (current > flow)
                            power.SidePowers[hex.Side] -= flow;
                        else {
                            val = power.SidePowers[hex.Side];
                            power.SidePowers[hex.Side] = 0;
                        }
                        this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Side = hex.Side, Flow = val }), h.EntityId.EntityId);
                    }
                }
            });
        }

        private void UpdateHexResource()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref resourceQuerySet.inter) == false)
                return;

            resourceDictionary.Clear();

            Entities.With(resourceQuerySet.group).ForEach((Entity entity,
                                                          ref HexPowerResource.Component resource,
                                                          ref Position.Component position) =>
            {
                var pos = position.Coords.ToUnityVector() + this.Origin;
                foreach (var kvp in this.HexDic)
                {
                    var index = kvp.Key;
                    if (HexUtils.IsInsideHex(this.Origin, index, pos, HexDictionary.HexEdgeLength) == false)
                        continue;

                    resourceDictionary.TryGetValue(index, out var current);
                    resourceDictionary[index] = current + resource.Level * 1.0f;    // todo HexDictionary
                }
            });
        }
    }
}
