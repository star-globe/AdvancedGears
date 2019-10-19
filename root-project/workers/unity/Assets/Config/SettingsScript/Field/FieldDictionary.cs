using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Field Dictionary", order = 0)]
    public class FieldDictionary : DictionarySettings
    {
        public static FieldDictionary Instance { get; private set; }

        [SerializeField] private FieldSettings[] fieldsList;
        readonly Dictionary<FieldWorkerType, FieldSettings> fieldDic = new Dictionary<FieldWorkerType, FieldSettings>();

        [SerializeField] private int standardResolution;
        public int StandardResolution => standardResolution;

        [SerializeField] private float standardSize;

        [SerializeField] private float maxHeight;
        public float MaxHeight => maxHeight;
        public static float WorldHeight => Instance.MaxHeight;

        [SerializeField] private float maxRange;
        public float MaxRange => maxRange;

        [SerializeField] private float queryRangeRate = 3.0f;
        [SerializeField] private float checkRangeRate = 0.5f;

        public static float CheckRangeRate => Instance.checkRangeRate;

        public static float QueryRange => Instance.MaxRange * Instance.queryRangeRate;

        [SerializeField]
        TerrainData baseTerrainData;
        public TerrainData BaseTerrainData => baseTerrainData;

        public override void Initialize()
        {
            Instance = this;

            foreach (var f in fieldsList)
                fieldDic[f.FieldWorkerType] = f;
        }

        public static FieldSettings Get(FieldWorkerType type)
        {
            if (Instance == null)
            {
                Debug.LogError("The Field Dictionary has not been set.");
                return null;
            }

            FieldSettings settings = null;
            Instance.fieldDic.TryGetValue(type, out settings);

            return settings;
        }

        public int GetResolution(float fieldSize)
        {
            return (int)(standardResolution * fieldSize / standardSize);
        }

        public float GetHeight(float fieldSize)
        {
            return maxHeight * fieldSize / standardSize;
        }

        public static int Count => Instance.fieldsList.Length;
    }
}
