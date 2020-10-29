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

        UnitSide currentSide;

        public void OnEnable()
        {
            statusReader.OnSideUpdate += OnSideUpdated;
            currentSide = statusReader.Data.Side;
        }

        private void OnSideUpdated(UnitSide side)
        {
            if (currentSide == side)
                return;

            currentSide = side;
            var index = facilityWriter.Data.HexIndex;
            //facilityWriter.SendUpdate(new HexFacility.Update()
            //{
            //    SideChanged = true,
            //});

            Debug.LogFormat("HexChanged! Index:{0} Side:{1}", index, side);
        }
    }
}
