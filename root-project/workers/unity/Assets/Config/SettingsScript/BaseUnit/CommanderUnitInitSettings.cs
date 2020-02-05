using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/CommanderUnit Config/InitSettings", order = 1)]
    public class CommanderUnitInitSettings : ScriptableObject
    {
        [SerializeField] float sightRange = 100.0f;
        public float SightRange => sightRange;
        [SerializeField] float allyRange = 50.0f;
        public float AllyRange => allyRange;
        [SerializeField] float inter = 3.0f;
        public float Inter => inter;

        [SerializeField] TeamInfo team;
        public TeamConfig TeamConfig => team.GetConfig();

        [SerializeField] float fowardLength = 10.0f;
        public float FowardLength => fowardLength;

        [SerializeField] float separeteWeight = 1.0f;
        public float SepareteWeight => separeteWeight;
        [SerializeField] float alignmentWeight = 1.0f;
        public float AlignmentWeight => alignmentWeight;
        [SerializeField] float cohesionWeight = 1.0f;
        public float CohesionWeight => cohesionWeight;

        [Serializable]
        class TeamInfo
        {
            public int soldiers;
            public int commanders;

            public TeamConfig GetConfig()
            {
                return new TeamConfig(soldiers,commanders);
            }
        }
    }
}
