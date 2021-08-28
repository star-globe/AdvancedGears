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
    public class HexPowerResourceConvertSystem : HexUpdateBaseSystem
    {
        EntityQuerySet resourceQuerySet;
        const int frequencyResource = 1;
        EntityQueryBuilder.F_EDDD<BaseUnitStatus.Component, ResourceComponent.Component, Position.Component> resourceAction;
        private Dictionary<EntityId, Dictionary<UnitSide, float>> reduceDictionary = new Dictionary<EntityId, Dictionary<UnitSide, float>>();

        protected override void OnCreate()
        {
            base.OnCreate();

            resourceQuerySet = new EntityQuerySet(GetEntityQuery(
                                                  ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                                  ComponentType.ReadOnly<ResourceComponent.Component>(),
                                                  ComponentType.ReadOnly<ResourceComponent.HasAuthority>(),
                                                  ComponentType.ReadOnly<Position.Component>()
                                                  ), frequencyResource, Time.ElapsedTime);
            resourceAction = ResourceQuery;
        }

        private void ResourceQuery(Entity entity,
                                    ref BaseUnitStatus.Component status,
                                    ref ResourceComponent.Component resource,
                                    ref Position.Component position)
        {
            var side = status.Side;
            if (side == UnitSide.None)
                return;

            int flow = 0;

            var pos = position.Coords.ToUnityVector() + this.Origin;
            foreach (var kvp in this.HexDic)
            {
                var index = kvp.Key;
                if (HexUtils.IsInsideHex(this.Origin, index, pos, HexDictionary.HexEdgeLength) == false)
                    continue;

                var hex = kvp.Value;
                var entityId = hex.EntityId.EntityId;
                if (reduceDictionary.ContainsKey(entityId) == false)
                    reduceDictionary[entityId] = new Dictionary<UnitSide, float>();

                var dic = reduceDictionary[entityId];
                dic.TryGetValue(status.Side, out var current);


                hex.Powers.TryGetValue(side, out var power);

                var diff = Mathf.Min(resource.ResourceMax - resource.Resource, HexDictionary.ResourceFlowMax);
                if (current + diff > power)
                    continue;

                dic[side] = current + diff;
                flow = diff;
                break;
            }

            if (flow > 0)
                resource.Resource = resource.Resource + flow;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref resourceQuerySet.inter) == false)
                return;

            foreach (var kvp in reduceDictionary)
                kvp.Value.Clear();

            Entities.With(resourceQuerySet.group).ForEach(resourceAction);

            foreach (var kvp in reduceDictionary) {
                var entityId = kvp.Key;
                foreach (var pair in kvp.Value) {
                    if (pair.Value > 0)
                        this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Side = pair.Key, Flow = -pair.Value }), entityId);
                }
            }
        }
    }
}
