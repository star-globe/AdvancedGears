using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class DictionaryPublisher : MonoBehaviour, ISettingsPublisher
    {
        [SerializeField] private BulletDictionary bulletDictionary;
        [SerializeField] private GunDictionary gunDictionary;


        public void Publish()
        {
            BulletDictionary.Instance = bulletDictionary;
            GunDictionary.Instance = gunDictionary;
        }
    }
}

