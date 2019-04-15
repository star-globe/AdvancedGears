using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class BaseUnitStateColor : MonoBehaviour
    {
        [Require] BaseUnitStatusReader reader;

        [SerializeField]
        Renderer stateRenderer;

        [SerializeField]
        StateColor[] stateColors;

        [SerializeField]
        Renderer sideRenderer;

        [SerializeField]
        SideColor[] sideColors;

        private void OnEnable()
        {
            reader.OnStateUpdate += UpdateState;
            UpdateState(reader.Data.State);
            UpdateSide(reader.Data.Side);
        }

        void UpdateState(UnitState state)
        {
            var col = UnityEngine.Color.white;
            var st = stateColors.FirstOrDefault(s => s.state == state);
            if (st != null)
                col = st.col;

            stateRenderer.material.color = col;
        }

        void UpdateSide(UnitSide side)
        {
            var col = UnityEngine.Color.white;
            var st = sideColors.FirstOrDefault(s => s.side == side);
            if (st != null)
                col = st.col;

            sideRenderer.material.color = col;
        }
    }

    [Serializable]
    internal class BaseColor
    {
        public UnityEngine.Color col;
    }

    [Serializable]
    internal class StateColor : BaseColor
    {
        public UnitState state;
    }

    [Serializable]
    internal class SideColor : BaseColor
    {
        public UnitSide side;
    }
}
