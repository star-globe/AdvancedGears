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

        Dictionary<uint, BulletSettings> dic = null;
        Dictionary<uint, BulletSettings> Dic
        {
            get
            {
                if (dic == null)
                {
                    dic = new Dictionary<uint, BulletSettings>();
                    foreach (var bullet in bulletsList)
                    {
                        if (dic.ContainsKey(bullet.TypeId) == false)
                            dic.Add(bullet.TypeId, bullet);
                    }
                }

                return dic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static BulletSettings Get(uint bulletId)
        {
            if (Instance == null)
            {
                Debug.LogError("The Bullet Dictionary has not been set.");
                return null;
            }

            if (Instance.Dic.ContainsKey(bulletId) == false)
            {
                Debug.LogErrorFormat("The id {0} doesn't exist in the dictionary", bulletId);
                return null;
            }

            return Instance.bulletsList[bulletId];
        }

        public static int Count => Instance.bulletsList.Length;
    }
}
