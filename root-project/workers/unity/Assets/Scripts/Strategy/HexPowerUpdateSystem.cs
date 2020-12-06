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
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdateHexPower();
        }

        const float flowValue = 3.0f;

        private void UpdateHexPower()
        {
            if (CheckTime(ref interAccess) == false)
                return;

            frontLineDic.Clear();
            foreach (var side in HexUtils.AllSides)
            {
                frontLineDic[side] = BorderHexList(side);
            }

            Entities.With(hexpowerGroup).ForEach((Entity entity,
                                      ref HexPower.Component power,
                                      ref HexBase.Component hex,
                                      ref SpatialEntityId entityId) =>
            {
                int bit = 0;
                int count = -1;
                var ids = HexUtils.GetNeighborHexIndexes(hex.Index);
                foreach (var id in ids)
                {
                    count++;

                    if (base.hexDic.ContainsKey(id) == false)
                        continue;

                    var h = base.hexDic[id];
                    if (h.Side != hex.Side)
                    {
                        bit += 1 << count;
                    }
                    else if (power.RealizedPower + power.StackPower > h.TotalPower &&
                             power.StackPower >= flowValue)
                    {
                        power.StackPower -= flowValue;
                        this.UpdateSystem.SendEvent(new HexPower.HexPowerFlow.Event(new HexPowerFlow() { Flow = flowValue }), h.EntityId.EntityId);
                    }
                }

                power.FrontBits = bit;
            });
        }
    }
}
