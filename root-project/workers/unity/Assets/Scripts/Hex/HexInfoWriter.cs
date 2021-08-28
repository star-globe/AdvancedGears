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

        private UnitSide Side => hexBase.Data.Side;

        const float attackRate = 0.2f;
        const float reduceRate = 3.0f;

        private readonly Dictionary<UnitSide, float> reducePowers = new Dictionary<UnitSide, float>();
        private readonly List<UnitSide> keyList = new List<UnitSide>();

        void HexPowerFlowEvent(HexPowerFlow powerFlow)
        {
            var powers = power.Data.SidePowers;
#if false

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

            foreach (var k in keys)
            {
                if (powers[k] < 0)
                    powers[k] = 0;
            }
#else
            var side = powerFlow.Side;
            if (powers.ContainsKey(side))
                powers[side] += powerFlow.Flow;
            else
                powers.Add(side, powerFlow.Flow);

            reducePowers.Clear();
            keyList.Clear();
            keyList.AddRange(powers.Keys);

            foreach (var k in keyList) {
                reducePowers[k] = GetReducePowers(k, powers);
            }

            foreach (var kvp in reducePowers) {
                if (powers.ContainsKey(kvp.Key) == false)
                    continue;

                powers[kvp.Key] -= kvp.Value;
            }

            foreach (var k in keyList)
            {
                if (powers[k] < 0)
                    powers[k] = 0;
            }
#endif
            power.SendUpdate(new HexPower.Update()
            {
                SidePowers = powers,
            });
        }

        private float GetReducePowers(UnitSide side, Dictionary<UnitSide, float> powers)
        {
            float pow = 0;

            foreach (var kvp in powers) {
                if (kvp.Key != side)
                    pow += kvp.Value;
            }

            if (this.Side != side)
                pow *= reduceRate;

            return pow;
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
