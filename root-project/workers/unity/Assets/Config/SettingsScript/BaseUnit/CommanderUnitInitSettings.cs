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
        [SerializeField] float allyRange = 50.0f;
        [SerializeField] float inter = 3.0f;

        [SerializeField] TeamInfo team;

        public float SightRange => sightRange;
        public float AllyRange => allyRange;
        public float Inter => inter;

        public TeamConfig TeamConfig => team.GetConfig();

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
