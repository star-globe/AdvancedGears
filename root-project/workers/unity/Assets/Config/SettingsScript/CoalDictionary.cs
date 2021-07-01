using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Coal Config/Coal Dictionary", order = 0)]
    public class CoalDictionary : DictionarySettings
    {
        public static CoalDictionary Instance { private get; set; }

        [SerializeField] private CoalSettings[] coalsList;

        public override void Initialize()
        {
            Instance = this;
        }

        public static CoalSettings Get(uint typeId)
        {
            if (Instance == null)
            {
                Debug.LogError("The Coal Dictionary has not been set.");
                return null;
            }

            if (typeId >= Count)
            {
                Debug.LogErrorFormat("The index {0} is outside of the dictionary's range (size {1}).", typeId, Count);
                return null;
            }

            return Instance.coalsList[typeId];
        }

        public static int Count => Instance.coalsList.Length;
    }
}
