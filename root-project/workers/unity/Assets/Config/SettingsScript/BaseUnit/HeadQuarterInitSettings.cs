using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/HeadQuarter Config/InitSettings", order = 1)]
    public class HeadQuarterInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        [SerializeField] uint maxRank = 3;
        [SerializeField] float inter = 3.0f;

        public float SightRange => sightRange;
        public uint MaxRank => maxRank;
        public float Inter => inter;
    }
}
