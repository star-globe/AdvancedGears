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
        [SerializeField] private UnitSpriteSettings[] spriteSettings;

        [SerializeField] private BaseUnitStateColorSettings colorSettings;

        Dictionary<UIType, GameObject> uiDic = null;
        Dictionary<UIType, GameObject> UIDic
        {
            get
            {
                uiDic = uiDic ?? UIObjectSettings.GetDictionary(uiSettings);
                return uiDic;
            }
        }

        Dictionary<UnitType, Sprite> unitDic = null;
        Dictionary<UnitType, Sprite> UnitDic
        {
            get
            {
                unitDic = unitDic ?? UnitSpriteSettings.GetDictionary(spriteSettings);
                return unitDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static GameObject GetUIObject(UIType uiType)
        {
            if (Instance.UIDic.TryGetValue(uiType, out var uiObject)) {
                return uiObject;
            }

            return null;
        }

        public static UnityEngine.Color GetSideColor(UnitSide side)
        {
            if (Instance.colorSettings == null)
                return UnityEngine.Color.white;

            return Instance.colorSettings.GetSideColor(side);
        }

        public static Sprite GetUnitSprite(UnitType type)
        {
            if (Instance.UnitDic.TryGetValue(type, out var sprite) == false)
                return null;

            return sprite;
        }

        [Serializable]
        public class UIObjectSettings : UISettings<UIType, GameObject>
        {
            [SerializeField]
            UIType type;
            public override UIType key => type;

            [SerializeField]
            GameObject uiObject;
            public override GameObject value => uiObject;
        }

        [Serializable]
        public class UnitSpriteSettings : UISettings<UnitType, Sprite>
        {
            [SerializeField]
            UnitType type;
            public override UnitType key => type;

            [SerializeField]
            Sprite sprite;
            public override Sprite value => sprite;
        }
    }

    [Serializable]
    public abstract class UISettings<Key, Value>
    {
        public abstract Key key { get; }
        public abstract Value value { get; }

        public static Dictionary<Key, Value> GetDictionary(UISettings<Key, Value>[] settings)
        {
            var dic = new Dictionary<Key, Value>();

            foreach (var set in settings)
            {
                if (dic.ContainsKey(set.key))
                    continue;

                dic.Add(set.key, set.value);
            }

            return dic;
        }
    }
}
