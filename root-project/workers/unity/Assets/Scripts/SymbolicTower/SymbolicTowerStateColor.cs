using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class SymbolicTowerStateColor : MonoBehaviour
    {
        [Require] SymbolicTowerReader reader;

        //[SerializeField]
        //Renderer stateRenderer;

        [SerializeField]
        Renderer sideRenderer;

        private void OnEnable()
        {
            //reader.OnStateUpdate += UpdateState;
            reader.OnSideUpdate += UpdateSide;
            //UpdateState(reader.Data.State);
            UpdateSide(reader.Data.Side);
        }

        //void UpdateState(UnitState state)
        //{
        //    stateRenderer.material.color = ColorDictionary.GetStateColor(state);
        //}

        void UpdateSide(UnitSide side)
        {
            sideRenderer.material.color = ColorDictionary.GetSideColor(side);
        }
    }
}
