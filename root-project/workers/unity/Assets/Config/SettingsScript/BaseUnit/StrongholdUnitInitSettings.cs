using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/StrongholdUnit Config/InitSettings", order = 1)]
    public class StrongholdUnitInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        [SerializeField] float dominationRange = 40.0f;
        [SerializeField] int maxStamina = 100;
        [SerializeField] float inter = 3.0f;

        public float SightRange => sightRange;
        public float Inter => inter;
        public float DominationRange => dominationRange;
        public int MaxStamina => maxStamina;
    }
}
