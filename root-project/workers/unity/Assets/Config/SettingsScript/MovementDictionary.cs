using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/Movement Dictionary", order = 0)]
    public class MovementDictionary : DictionarySettings
    {
        public static MovementDictionary Instance { private get; set; }

        [SerializeField] private MovementSettings[] movementSettings;

        [SerializeField]
        float rotateLimitRate = 15.0f;
        public static float RotateLimitRate => Instance.rotateLimitRate;


        Dictionary<UnitType, MovementSettings> movementDic = null;
        Dictionary<UnitType, MovementSettings> MovementDic
        {
            get
            {
                if (movementDic == null) {
                    movementDic = new Dictionary<UnitType, MovementSettings>();

                    foreach (var set in movementSettings)
                    {
                        if (movementDic.ContainsKey(set.type))
                            continue;

                       movementDic.Add(set.type, set);
                    }
                }

                return movementDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static bool TryGet(UnitType unitType, out float speed, out float rot)
        {
            speed = 0.0f;
            rot = 0.0f;
            if (Instance.MovementDic.TryGetValue(unitType, out var settings)) {
                speed = settings.speed;
                rot = settings.rot;
                return true;
            }

            return false;
        }

        [Serializable]
        public class MovementSettings
        {
            public UnitType type;
            public float speed;
            public float rot;
        }
    }
}
