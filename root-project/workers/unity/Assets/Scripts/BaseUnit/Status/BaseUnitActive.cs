using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class BaseUnitActive : MonoBehaviour
    {
        [Require] BaseUnitStatusReader reader;

        private void OnEnable()
        {
            reader.OnStateUpdate += UpdateState;
        }

        void UpdateState(UnitState state)
        {
            this.gameObject.SetActive(state != UnitState.Sleep);
        }
    }
}
