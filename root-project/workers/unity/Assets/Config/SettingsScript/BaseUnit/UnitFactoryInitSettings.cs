using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/UnitFactory Config/InitSettings", order = 1)]
    public class UnitFactoryInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        [SerializeField] int resourceMax = 100;
        [SerializeField] float inter = 3.0f;

        public float SightRange => sightRange;
        public int ResourceMax => resourceMax;
        public float Inter => inter;
    }
}
