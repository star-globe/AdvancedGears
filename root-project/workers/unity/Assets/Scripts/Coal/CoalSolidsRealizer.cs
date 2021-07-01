using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class CoalSolidsRealizer : MonoBehaviour
    {
        [Require] CoalSolidsReader reader;

        private void OnEnable()
        {
            //reader.//On += PostureChanged;

            // initialize
        }
    }
}
