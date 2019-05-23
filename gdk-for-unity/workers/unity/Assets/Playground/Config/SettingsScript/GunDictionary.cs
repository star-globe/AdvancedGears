using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(menuName = "AdvancedGears/Gun Config/Gun Dictionary", order = 0)]
    public class GunDictionary : ScriptableObject
    {
        public static GunDictionary Instance { private get; set; }

        [SerializeField] private GunSettings[] gunsList;

        Dictionary<int,GunSettings> dic = null;
        Dictionary<int,GunSettings> Dic
        {
            get
            {
                if (dic == null)
                {
                    dic = new Dictionary<int,GunSettings>();
                    foreach (var gun in gunsList)
                    {
                        if (dic.ContainsKey(gun.TypeId) == false)
                            dic.Add(gun.TypeId, gun);
                    }
                }

                return dic;
            }
        }

        public static GunSettings GetGunSettings(int gunId)
        {
            if (Instance == null)
            {
                Debug.LogError("The Bullet Dictionary has not been set.");
                return null;
            }

            if (Instance.Dic.ContainsKey(gunId) == false)
            {
                Debug.LogErrorFormat("The id {0} doesn't exist in the dictionary", index);
                return null;
            }

            return Instance.Dic[gunID];
        }
    }
}
