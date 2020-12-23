using System.Linq;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class HexInfoWriter : MonoBehaviour
    {
        [Require] HexBaseWriter hexBase;
        [Require] HexPowerWriter power;

        private void OnEnable()
        {
            hexBase.OnSideChangedEvent += SideChangedEvent;
            power.OnHexPowerFlowEvent += HexPowerFlowEvent;
            power.OnHexActiveChangeEvent += HexActiveChangeEvent;
        }

        void SideChangedEvent(SideChangedEvent sideChanged)
        {
            hexBase.SendUpdate(new HexBase.Update()
            {
                Side = sideChanged.Side,
            });
        }

        const float attackRate = 0.55f;

        void HexPowerFlowEvent(HexPowerFlow powerFlow)
        {
            var powers = power.Data.SidePowers;
            var keys = powers.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
            var flow = powerFlow.Flow * attackRate;
            if (keys.Length > 0)
            {
                foreach (var k in keys)
                {
                    if (powerFlow.Side == k) {
                        continue;
                    }
                    else {
                        if (powers[k] >= flow) {
                            powers[k] -= flow;
                            flow = 0;
                        }
                        else
                        {
                            flow -= powers[k];
                            powers[k] = 0;
                        }
                    }

                    if (flow == 0)
                        break;
                }
            }

            var add = flow + powerFlow.Flow * (1.0f - attackRate);
            if (powers.ContainsKey(powerFlow.Side) == false)
                powers[powerFlow.Side] = add;
            else
                powers[powerFlow.Side] += add;

            power.SendUpdate(new HexPower.Update()
            {
                SidePowers = powers,
            });
        }

        void HexActiveChangeEvent(HexActiveChange change)
        {
            power.SendUpdate(new HexPower.Update()
            {
                IsActive = change.IsActive,
            });
        }
    }
}
