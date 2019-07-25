using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Bullet Config/Bullet Dictionary", order = 0)]
    public class BulletDictionary : ScriptableObject
    {
        public static BulletDictionary Instance { private get; set; }

        [SerializeField] private BulletSettings[] bulletsList;

        public static BulletSettings Get(int index)
        {
            if (Instance == null)
            {
                Debug.LogError("The Bullet Dictionary has not been set.");
                return null;
            }

            if (index < 0 || index >= Count)
            {
                Debug.LogErrorFormat("The index {0} is outside of the dictionary's range (size {1}).", index, Count);
                return null;
            }

            return Instance.bulletsList[index];
        }

        public static int Count => Instance.bulletsList.Length;
    }
}
