using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/UnitPhysics Dictionary", order = 0)]
    public class UnitPhysicsDictionary : DictionarySettings
    {
        public static UnitPhysicsDictionary Instance { private get; set; }

        [SerializeField] private UnitPhysicsSettings[] physicsSettgins;

        Dictionary<UnitType, UnitPhysicsSettings> physicsDic = null;
        Dictionary<UnitType, UnitPhysicsSettings> PhysicsDic
        {
            get
            {
                if (physicsDic == null) {
                    physicsDic = new Dictionary<UnitType, UnitPhysicsSettings>();

                    foreach (var set in physicsSettgins)
                    {
                        if (physicsDic.ContainsKey(set.type))
                            continue;

                       physicsDic.Add(set.type, set);
                    }
                }

                return physicsDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static bool IsBuilding(UnitType unitType)
        {
            if (Instance.PhysicsDic.TryGetValue(unitType, out var set))
                return set.isBuilding;

            return false;
        }

        [Serializable]
        public class UnitPhysicsSettings
        {
            public UnitType type;
            public bool isBuilding;
        }
    }
}
