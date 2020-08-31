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
            statusReader.OnForceStateEvent += OnForceState;
        }

        private void OnForceState(ForceStateChange change)
        {
            var index = facilityWriter.Data.HexIndex;
            facilityWriter.SendHexChangedEvent(new HexChangedEvent(index, change.Side));
        }
    }
}
