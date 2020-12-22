using System.Collections;
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
        }

        void SideChangedEvent(SideChangedEvent sideChanged)
        {
            hexBase.SendUpdate(new HexBase.Update()
            {
                Side = sideChanged.Side,
            });
        }

        void HexPowerFlowEvent(HexPowerFlow powerFlow)
        {
            var powers = power.Data.SidePowers;
            var keys = powers.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key).ToArray();
            var flow = powerFlow.Flow / 2;
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

            var side = powerFlow.Side;
            var add = flow + powerFlow / 2;
            if (powers.ContainsKey(side) == false)
                powers[side] = add;
            else
                powers[side] += add;

            power.SendUpdate(new HexPower.Update()
            {
                SidePowers = powers,
            });
        }
    }
}
