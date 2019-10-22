using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Bullet Dictionary", order = 0)]
    class TowerDictionary : DictionarySettings
    {
        public static TowerDictionary Instance { get; private set; }

        [SerializeField]
        SymbolicTowerSettings[] towerList;
        readonly Dictionary<UnitSide, SymbolicTowerSettings> towerDic = new Dictionary<UnitSide, SymbolicTowerSettings>();

        [SerializeField]
        float intervalRate = 1.0f / 100;
        public static float IntervalRate { get { return Instance.intervalRate; } }

        [SerializeField]
        float dispLength = 700.0f;
        public static float DispLength { get { return Instance.dispLength; } }

        public override void Initialize()
        {
            Instance = this;

            foreach (var t in towerList)
                towerDic[t.Side] = t;
        }

        public static SymbolicTowerSettings Get(UnitSide side)
        {
            if (Instance == null)
            {
                Debug.LogError("The Tower Dictionary has not been set.");
                return null;
            }

            SymbolicTowerSettings settings = null;
            Instance.towerDic.TryGetValue(side, out settings);

            return settings;
        }
    }
}
