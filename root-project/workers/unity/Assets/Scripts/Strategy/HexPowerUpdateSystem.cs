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
    public class HexPowerUpdateSystem : HexUpdateBaseSystem
    {
        EntityQuerySet hexpowerQuerySet;
        EntityQuerySet resourceQuerySet;
        const int frequencyPower = 2;
        const int frequencyResource = 1;
        EntityQueryBuilder.F_EDDD<HexPower.Component, HexBase.Component, SpatialEntityId> powerAction;
        EntityQueryBuilder.F_EDDD<BaseUnitStatus.Component, HexPowerResource.Component, Position.Component> resourceAction;

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
                                                  ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                  ComponentType.ReadOnly<HexPowerResource.Component>(),
                                                  ComponentType.ReadOnly<Position.Component>()
                                                  ), frequencyResource, Time.ElapsedTime);
            powerAction = PowerQuery;
            resourceAction = ResourceQuery;
        }

        readonly Dictionary<uint, Dictionary<UnitSide,float>> resourceDictionary = new Dictionary<uint, Dictionary<UnitSide, float>>();
        readonly Dictionary<EntityId, Dictionary<UnitSide,float>> flowDictionary = new Dictionary<EntityId, Dictionary<UnitSide,float>>();
        readonly List<uint> targetIds = new List<uint>();

        protected override void OnUpdate()
        {
            UpdateHexResource();

            UpdateHexPower();
        }

        const float flowValueRate = 0.05f;
        double deltaTime = 0;

        private void UpdateHexPower()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref hexpowerQuerySet.inter) == false)
                return;

            deltaTime = hexpowerQuerySet.GetDelta(Time.ElapsedTime);

            foreach (var kvp in flowDictionary)
                kvp.Value.Clear();

            Entities.With(hexpowerQuerySet.group).ForEach(powerAction);

            foreach (var kvp in flowDictionary) {
                var power = kvp.Value;
                foreach (var pair in power) {
                    if (pair.Value > 0)
                        this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Side = pair.Key, Flow = pair.Value }), kvp.Key);
                }
            }
        }

        readonly List<UnitSide> keys = new List<UnitSide>();

        private void PowerQuery(Entity entity,
                                ref HexPower.Component power,
                                ref HexBase.Component hex,
                                ref SpatialEntityId entityId)
        {
            keys.Clear();
            keys.AddRange(power.SidePowers.Keys);

            foreach (var key in keys) {
                var current = power.SidePowers[key];
                if (current <= 0)
                    power.SidePowers[key] = 0;
            }

            if (resourceDictionary.TryGetValue(hex.Index, out var sideResource)) {
                foreach (var kvp in sideResource) {
                    power.SidePowers.TryGetValue(kvp.Key, out var current);
                    power.SidePowers[kvp.Key] = Mathf.Min(current + (float) (kvp.Value * deltaTime), HexDictionary.HexPowerLimit);
                }
            }

            var hexSide = hex.Side;
            if (hexSide == UnitSide.None)
                return;

            if (power.SidePowers.TryGetValue(hexSide, out var selfPower) == false)
                return;

            var ids = HexUtils.GetNeighborHexIndexes(hex.Index);
            targetIds.Clear();
            foreach (var id in ids)
            {
                if (base.HexDic.ContainsKey(id) == false)
                    continue;

                var h = base.HexDic[id];
                if (h.Attribute == HexAttribute.NotBelong)
                    continue;

                bool isFlow = false;
                if (h.Side == hexSide)
                {
                    if (selfPower > h.CurrentPower + selfPower * flowValueRate)
                    {
                        isFlow = true;
                    }
                }
                else //if (h.Side == UnitSide.None)
                {
                    isFlow = true;
                }

                if (isFlow)
                    targetIds.Add(id);
            }

            var totalFlow = (float) (flowValueRate * deltaTime * selfPower);
            var count = targetIds.Count;
            var flow = totalFlow / count;

            foreach (var id in targetIds)
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
        }

        private void UpdateHexResource()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref resourceQuerySet.inter) == false)
                return;

            foreach (var kvp in resourceDictionary)
                kvp.Value.Clear();

            Entities.With(resourceQuerySet.group).ForEach(resourceAction);
        }

        private void ResourceQuery(Entity entity,
                                    ref BaseUnitStatus.Component status,
                                    ref HexPowerResource.Component resource,
                                    ref Position.Component position)
        {
            if (status.Side == UnitSide.None)
                return;

            var pos = position.Coords.ToUnityVector() + this.Origin;
            foreach (var kvp in this.HexDic)
            {
                var index = kvp.Key;
                var center = HexUtils.GetHexCenter(this.Origin, index, HexDictionary.HexEdgeLength);

                if ((center - pos).magnitude > HexDictionary.HexEdgeLength / 2)
                    continue;

                if (resourceDictionary.ContainsKey(index) == false)
                    resourceDictionary[index] = new Dictionary<UnitSide, float>();

                var dic = resourceDictionary[index];
                dic.TryGetValue(status.Side, out var current);
                dic[status.Side] = current + resource.Level * HexDictionary.HexResourceRate;
            }
        }
    }
}
