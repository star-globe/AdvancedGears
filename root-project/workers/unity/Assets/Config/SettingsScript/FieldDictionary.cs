using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Field Config/Field Dictionary", order = 0)]
    public class FieldDictionary : DictionarySettings
    {
        public static FieldDictionary Instance { private get; set; }

        [SerializeField] private FieldSettings[] fieldsList;

        readonly Dictionary<FieldWorkerType, FieldSettings> fieldDic = new Dictionary<FieldWorkerType, FieldSettings>();

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

        public static int Count => Instance.fieldsList.Length;
    }
}
