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

        public override void Initialize()
        {
            Instance = this;
        }

        public static FieldSettings Get(int index)
        {
            if (Instance == null)
            {
                Debug.LogError("The Field Dictionary has not been set.");
                return null;
            }

            if (index < 0 || index >= Count)
            {
                Debug.LogErrorFormat("The index {0} is outside of the dictionary's range (size {1}).", index, Count);
                return null;
            }

            return Instance.fieldsList[index];
        }

        public static int Count => Instance.fieldsList.Length;
    }
}
