using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class CoalSolidsInfoWriter : MonoBehaviour
    {
        [Require] CoalSolidsWriter writer;

        private void OnEnable()
        {
            //reader.//On += PostureChanged;

            // initialize
        }
    }
}
