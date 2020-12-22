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
        EntityQuery hexpowerGroup;
        IntervalChecker interAccess;
        const int frequencyPower = 5;

        double deltaTime = -1.0;

        readonly Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>> frontLineDic = new Dictionary<UnitSide, Dictionary<uint, List<FrontLineInfo>>>();

        protected override void OnCreate()
        {
            base.OnCreate();

            hexpowerGroup = GetEntityQuery(
                ComponentType.ReadWrite<HexPower.Component>(),
                ComponentType.ReadOnly<HexPower.HasAuthority>(),
                ComponentType.ReadOnly<HexBase.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            interAccess = IntervalCheckerInitializer.InitializedChecker(1.0f / frequencyPower);

            deltaTime = Time.ElapsedTime;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdateHexPower();
        }

        const float flowValue = 1.0f;
        const float resourceValue = 1.0f;

        private void UpdateHexPower()
        {
            if (CheckTime(ref interAccess) == false)
                return;

            deltaTime = Time.ElapsedTime - deltaTime;

            Entities.With(hexpowerGroup).ForEach((Entity entity,
                                      ref HexPower.Component power,
                                      ref HexBase.Component hex,
                                      ref SpatialEntityId entityId) =>
            {
                if (hex.Side == UnitSide.None)
                    return;

                power.SidePowers.TryGetValue(hex.Side, out var current);

                if (hex.Attribute == HexAttribute.CentralBase) {
                    current += (float)(resourceValue * deltaTime);
                    power.SidePowers[hex.Side] = current;
                }

                var flow = (float)(flowValue * deltaTime);

                int count = -1;
                var ids = HexUtils.GetNeighborHexIndexes(hex.Index);
                foreach (var id in ids)
                {
                    count++;

                    if (base.hexDic.ContainsKey(id) == false)
                        continue;

                    var h = base.hexDic[id];
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
                            val = power.Sidepowers[hex.Side];
                            power.Sidepowers[hex.Side] = 0;
                        }
                        this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Side = hex.Side, Flow = val }), h.EntityId.EntityId);
                    }
                }
            });
        }
    }
}
