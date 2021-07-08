using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdvancedGears;

namespace AdvancedGears.UI
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/UIGameState Dictionary", order = 0)]
    public class UIGameStateDictionary : DictionarySettings
    {
        public static UIGameStateDictionary Instance { private get; set; }

        [SerializeField] private UIGameStateSettings[] uiGameStateSettings;

        Dictionary<GameState, UIGameStateObject> uiDic = null;
        Dictionary<GameState, UIGameStateObject> UIDic
        {
            get
            {
                uiDic = uiDic ?? UIGameStateSettings.GetDictionary(uiGameStateSettings);
                return uiDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static UIGameStateObject GetUIObject(GameState state)
        {
            if (Instance.UIDic.TryGetValue(state, out var uiObject)) {
                return uiObject;
            }

            return null;
        }

        [Serializable]
        public class UIGameStateSettings : UISettings<GameState, UIGameStateObject>
        {
            [SerializeField]
            GameState state;
            public override GameState key => state;

            [SerializeField]
            UIGameStateObject uiObject;
            public override UIGameStateObject value => uiObject;
        }
    }
}
