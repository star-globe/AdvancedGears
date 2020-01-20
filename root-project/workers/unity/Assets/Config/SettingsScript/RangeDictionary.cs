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

        [SerializeField] private double unitInter = 3.0;
        public static double UnitInter => Instance.unitInter;

        [SerializeField] private float moveRangeRate = 0.7f;
        public static float MoveRangeRate => Instance.moveRangeRate;

        [SerializeField] private float strategyRangeRate = 100.0f;
        public static float StrategyRangeRate => Instance.strategyRangeRate;

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
