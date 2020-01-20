using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/UnitFactory Dictionary", order = 0)]
    public class UnitFactoryDictionary : DictionarySettings
    {
        public static UnitFactoryDictionary Instance { private get; set; }

        [SerializeField] private CostSettings[] costSettings;

        Dictionary<UnitType, CostSettings> costDic = null;
        Dictionary<FUnitype, CostSettings> CostDic
        {
            get
            {
                if (costDic == null) {
                    costDic = new Dictionary<FUnitype, CostSettings>();

                    foreach (var set in costSettings)
                    {
                        if (costDic.ContainsKey(set.type))
                            continue;

                       costDic.Add(set.type, set);
                    }
                }

                return costDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static bool TryGetCost(UnitType unitType, out int resourceCost, out float timeCost)
        {
            resourceCost = 0;
            timeCost = 0;
            if (Instance.RangeDic.TryGetValue(rangeType, out var set)) {
                resourceCost = set.resourceCost;
                timeCost = set.timeCost;
                return true;
            }

            return false;
        }

        [Serializable]
        public class CostSettings
        {
            public UnitType type;
            public int resorceCost;
            public float timeCost;
        }
    }
}
