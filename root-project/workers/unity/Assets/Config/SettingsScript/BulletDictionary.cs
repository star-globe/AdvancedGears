using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Bullet Config/Bullet Dictionary", order = 0)]
    public class BulletDictionary : DictionarySettings
    {
        public static BulletDictionary Instance { private get; set; }

        [SerializeField] private BulletSettings[] bulletsList;

        public override void Initialize()
        {
            Instance = this;
        }

        public static BulletSettings Get(uint gunId)
        {
            if (Instance == null)
            {
                Debug.LogError("The Bullet Dictionary has not been set.");
                return null;
            }

            if (gunId >= Count)
            {
                Debug.LogErrorFormat("The index {0} is outside of the dictionary's range (size {1}).", gunId, Count);
                return null;
            }

            return Instance.bulletsList[gunId];
        }

        public static int Count => Instance.bulletsList.Length;
    }
}
