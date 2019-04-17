using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace Playground
{
    public class CommanderUnitInitializer : MonoBehaviour
    {
        [Require] CommanderSightWriter sight;

        float inter = 1.5f;

        [SerializeField]
        float sightRange = 100.0f;

        void Start()
        {
            sight.SendUpdate(new CommanderSight.Update
            {
                Interval = inter,
                LastSearched = 0,
                Range = sightRange
            });
        }
    }
}
