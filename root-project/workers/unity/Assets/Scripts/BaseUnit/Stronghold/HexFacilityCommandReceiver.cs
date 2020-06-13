using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable;

namespace AdvancedGears
{
    public class HexFacilityCommandReceiver : MonoBehaviour
    {
		[Require] World world;
        [Require] HexFacilityReader facilityReader;
        [Require] BaseUnitStatusReader statusReader;

        HexUpdateSystem hexUpdateSystem = null;

        public void OnEnable()
        {
            hexUpdateSystem = world.GetExistingSystem<HexUpdateSystem>();
            statusReader.OnSideUpdate += OnSideUpdated;
            statusReader.OnStateUpdate += OnStateChanged;
        }

        private void OnSideUpdated(UnitSide side)
        {
            if (hexUpdateSystem != null)
                hexUpdateSystem.HexChanged(facilityReader.Data.HexIndex, side);
        }

        private void OnStateChanged(UnitState state)
        {
            if (state != UnitState.Dead)
                return;

            OnSideUpdated(UnitSide.None);
        }
    }
}
