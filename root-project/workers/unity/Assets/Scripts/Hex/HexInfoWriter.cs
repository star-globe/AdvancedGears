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
            if (powers.ContainsKey(powerFlow.Side))
                powers[powerFlow.Side] += powerFlow.Flow;
            else
                powers[powerFlow.Side] = powerFlow.Flow;

            power.SendUpdate(new HexPower.Update()
            {
                SidePowers = powers,
            });
        }
    }
}
