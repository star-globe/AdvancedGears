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
        readonly Dictionary<EntityId, Dictionary<UnitSide,float>> flowDictionary = new Dictionary<EntityId, Dictionary<UnitSide,float>>();

        protected override void OnUpdate()
        {
            UpdateHexResource();

            UpdateHexPower();
        }

        const float flowValueRate = 0.05f;

        private void UpdateHexPower()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref hexpowerQuerySet.inter) == false)
                return;

            var deltaTime = hexpowerQuerySet.GetDelta(Time.ElapsedTime);

            foreach (var kvp in flowDictionary)
                kvp.Value.Clear();

            Entities.With(hexpowerQuerySet.group).ForEach((Entity entity,
                                                           ref HexPower.Component power,
                                                           ref HexBase.Component hex,
                                                           ref SpatialEntityId entityId) =>
            {
                var hexSide = hex.Side;
                if (hexSide == UnitSide.None)
                    return;

                power.SidePowers.TryGetValue(hexSide, out var current);

                if (current <= 0) {
                    power.SidePowers[hexSide] = 0;
                    current = 0;
                }

                if (resourceDictionary.TryGetValue(hex.Index, out var resourceValue))
                    power.SidePowers[hexSide] = current + (float)(resourceValue * deltaTime);

                var ids = HexUtils.GetNeighborHexIndexes(hex.Index).Where(id =>
                {
                    if (base.HexDic.ContainsKey(id) == false)
                        return false;

                    var h = base.HexDic[id];
                    bool isFlow = false;
                    if (h.Side == hexSide)
                    {
                        if (current > h.CurrentPower + current * flowValueRate)
                        {
                            isFlow = true;
                        }
                    }
                    else if (h.Side == UnitSide.None)
                    {
                        isFlow = true;
                    }

                    return isFlow;
                });

                var totalFlow = (float) (flowValueRate * deltaTime * current);
                var count = ids.Count();
                var flow = totalFlow / count;

                foreach (var id in ids)
                {
                    float val = flow;
                    if(power.SidePowers[hexSide] > flow)
                       power.SidePowers[hexSide] -= flow;
                    else {
                        val = power.SidePowers[hexSide];
                        power.SidePowers[hexSide] = 0;
                    }

                    var h = base.HexDic[id];
                    var key = h.EntityId.EntityId;
                    if (flowDictionary.ContainsKey(key))
                    {
                        var powerDic = flowDictionary[key];
                        if (powerDic.ContainsKey(hexSide))
                            powerDic[hexSide] += val;
                        else
                            powerDic[hexSide] = val;
                    }
                    else
                    {
                        flowDictionary[key] = new Dictionary<UnitSide, float>();
                        flowDictionary[key].Add(hexSide, val);
                    }
                }
            });

            foreach (var kvp in flowDictionary)
            {
                var power = kvp.Value;
                foreach (var pair in power.Where(p => p.Value > 0))
                {
                    this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Side = pair.Key, Flow = pair.Value }), kvp.Key);
                }
            }
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
                    resourceDictionary[index] = current + resource.Level * 3.0f;    // todo HexDictionary
                }
            });
        }
    }
}
