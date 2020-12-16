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
        [Require] HexBaseWriter writer;
        [Require] HexPowerWriter powerWriter;

        private void OnEnable()
        {
            writer.OnSideChangedEvent += SideChangedEvent;
            powerWriter.OnHexPowerFlowEvent += HexPowerFlowEvent;
        }

        void SideChangedEvent(SideChangedEvent sideChanged)
        {
            writer.SendUpdate(new HexBase.Update()
            {
                Side = sideChanged.Side,
            });
        }

        void HexPowerFlowEvent(HexPowerFlow powerFlow)
        {
            var powers = writer.Data.SidePowers;
            if (powers.ContainsKey(powerFlow.Side))
                powers[powerFlow.Side] += powerFlow.Flow;
            else
                powers[powerFlow.Side] = powerFlow.Flow;

            powerWriter.SendUpdate(new HexPower.Update()
            {
                SidePowers = powers,
            });
        }
    }
}
