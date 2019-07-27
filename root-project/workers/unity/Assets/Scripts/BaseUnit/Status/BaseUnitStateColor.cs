using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class BaseUnitStateColor : MonoBehaviour
    {
        [Require] BaseUnitStatusReader reader;

        [SerializeField]
        Renderer stateRenderer;

        [SerializeField]
        Renderer sideRenderer;

        [SerializeField]
        BaseUnitStateColorSettings colorSettings;

        private void OnEnable()
        {
            reader.OnStateUpdate += UpdateState;
            UpdateState(reader.Data.State);
            UpdateSide(reader.Data.Side);
        }

        void UpdateState(UnitState state)
        {
            stateRenderer.material.color = colorSettings.GetStateColor(state);
        }

        void UpdateSide(UnitSide side)
        {
            sideRenderer.material.color = colorSettings.GetSideColor(side);
        }
    }
}
