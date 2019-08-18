using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class DictionaryPublisher : MonoBehaviour, ISettingsPublisher
    {
        [SerializeField]
        List<DictionarySettings> dictionaries;

        public void Publish()
        {
            foreach (var dic in dictionaries)
                dic.Initialize();
        }
    }

    public abstract class DictionarySettings : ScriptableObject
    {
        public abstract void Initialize();
    }
}

