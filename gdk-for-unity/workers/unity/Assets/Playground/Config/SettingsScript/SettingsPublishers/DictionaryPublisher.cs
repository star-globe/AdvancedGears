using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class DictionaryPublisher : MonoBehaviour, ISettingsPublisher
    {
        [SerializeField] private BulletDictionary bulletDictionary;

        public void Publish()
        {
            BulletDictionary.Instance = bulletDictionary;
        }
    }
}

