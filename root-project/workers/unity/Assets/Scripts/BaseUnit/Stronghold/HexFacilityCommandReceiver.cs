using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable;

namespace AdvancedGears
{
    public class HexFacilityCommandReceiver : MonoBehaviour
    {
        [Require] HexFacilityWriter facilityWriter;
        [Require] BaseUnitStatusReader statusReader;

        public void OnEnable()
        {
            statusReader.OnSideUpdate += OnSideUpdated;
            statusReader.OnStateUpdate += OnStateChanged;
        }

        private void OnSideUpdated(UnitSide side)
        {
            var index = facilityWriter.Data.HexIndex;
            facilityWriter.SendHexChangedEvent(new HexChangedEvent(index,side));
        }

        private void OnStateChanged(UnitState state)
        {
            if (state != UnitState.Dead)
                return;

            OnSideUpdated(UnitSide.None);
        }
    }
}
