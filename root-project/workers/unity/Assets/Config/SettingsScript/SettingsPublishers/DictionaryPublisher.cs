using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class DictionaryPublisher : MonoBehaviour, ISettingsPublisher
    {
        [SerializeField]
        string dictionaryPath;
        public string DictionaryPath { get { return dictionaryPath; } }

        [SerializeField]
        List<DictionarySettings> dictionaries;

        public void Publish()
        {
            foreach (var dic in dictionaries)
                dic.Initialize();
        }

        public void ClearDictionaries()
        {
            dictionaries.Clear();
        }

        public void AddDictionary(DictionarySettings dic)
        {
            dictionaries.Add(dic);
        }
    }

    public abstract class DictionarySettings : ScriptableObject
    {
        public abstract void Initialize();
    }
}
