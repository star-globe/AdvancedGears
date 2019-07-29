using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/CommanderUnit Config/InitSettings", order = 1)]
    public class CommanderUnitInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        [SerializeField] float allyRange = 50.0f;
        [SerializeField] float inter = 3.0f;

        public float SightRange => sightRange;
        public float AllyRange => allyRange;
        public float Inter => inter;
    }
}
