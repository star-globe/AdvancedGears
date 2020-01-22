using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

namespace AdvancedGears.UI
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/UIObject Dictionary", order = 0)]
    public class UIObjectDictionary : DictionarySettings
    {
        public static UIObjectDictionary Instance { private get; set; }

        [SerializeField] private UIObjectSettings[] uiSettings;

        Dictionary<UIType, UIObjectSettings> uiDic = null;
        Dictionary<UIType, UIObjectSettings> UIDic
        {
            get
            {
                if (uiDic == null) {
                    uiDic = new Dictionary<UIType, UIObjectSettings>();

                    foreach (var set in uiSettings)
                    {
                        if (uiDic.ContainsKey(set.type))
                            continue;

                       uiDic.Add(set.type, set);
                    }
                }

                return uiDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static GameObject GetUIObject(UIType uiType)
        {
            if (Instance.UIDic.TryGetValue(uiType, out var set)) {
                return set.uiObject;
            }

            return null;
        }

        [Serializable]
        public class UIObjectSettings
        {
            public UIType type;
            public GameObject uiObject;
        }
    }
}
