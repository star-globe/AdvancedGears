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

        [SerializeField] private double teamInter = 15.0;
        public static double TeamInter => Instance.teamInter;

        [SerializeField] private float moveBufferRate = 0.2f;
        public static float MoveBufferRate => Instance.moveBufferRate;

        [SerializeField] private float sightRangeRate = 1.5f;
        public static float SightRangeRate => Instance.sightRangeRate;

        [SerializeField] private float strategyRangeRate = 100.0f;
        public static float StrategyRangeRate => Instance.strategyRangeRate;

        [SerializeField] private float uiRange = 100.0f;
        public static float UIRange => Instance.uiRange;

        [SerializeField] private float baseBoidsRange = 30.0f;
        public static float BaseBoidsRange => Instance.baseBoidsRange;

        [SerializeField] private float boidsRankRate = 3.0f;
        public static float BoidsRankRate => Instance.boidsRankRate;

        [SerializeField] private float minimapRange = 1000.0f;
        public static float MiniMapRange => Instance.minimapRange;

        [SerializeField] private float armyCloudRange = 1200.0f;
        public static float ArmyCloudRange => Instance.armyCloudRange;

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

        public static float GetBoidsRange(uint rank)
        {
            var baseRange = BaseBoidsRange;
            if (rank == 0)
                return baseRange;

            return baseRange * Mathf.Pow(BoidsRankRate, rank - 1);
        }

        [Serializable]
        public class RangeSettings
        {
            public FixedRangeType type;
            public float range;
        }
    }
}
