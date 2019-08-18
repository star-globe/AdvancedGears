using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/Range Dictionary", order = 0)]
    public class RangeDictionary : DictionarySettings
    {
        public static RangeDictionary Instance { private get; set; }

        [SerializeField] private RangeSettings[] rangeSettings;

        Dictionary<FixedRangeType, float> rangeDic = null;
        Dictionary<FixedRangeType, float> RangeDic
        {
            get
            {
                if (rangeDic == null) {
                    rangeDic = new Dictionary<FixedRangeType, float>();

                    foreach (var set in rangeSettings)
                    {
                        if (rangeDic.ContainsKey(set.type))
                            continue;

                       rangeDic.Add(set.type, set.range);
                    }
                }

                return rangeDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static float Get(FixedRangeType rangeType)
        {
            float range = 0;
            Instance.RangeDic.TryGetValue(rangeType, out range);

            return range;
        }

        [Serializable]
        public class RangeSettings
        {
            public FixedRangeType type;
            public float range;
        }
    }
}
