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
        MeshRenderer renderer;

        [SerializeField]
        StateColor[] stateColors;

        private void OnEnable()
        {
            reader.OnStateUpdate += UpdateState;
            UpdateState(reader.Data.State);
        }

        void UpdateState(UnitState state)
        {
            var col = UnityEngine.Color.white;
            var st = stateColors.FirstOrDefault(s => s.state == state);
            if (st != null)
                col = st.col;

            renderer.material.color = col;
        }
    }

    [Serializable]
    internal class StateColor
    {
        public UnitState state;
        public UnityEngine.Color col;
    }
}
