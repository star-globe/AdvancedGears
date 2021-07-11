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

        [SerializeField] private UIGameStateObject[] uiGameStateObjects;

        Dictionary<GameState, UIGameStateObject> uiDic = null;
        Dictionary<GameState, UIGameStateObject> UIDic
        {
            get
            {
                uiDic = uiDic ?? CreateDictionary(uiGameStateObjects);
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

        private Dictionary<GameState, UIGameStateObject> CreateDictionary(UIGameStateObject[] stateObjects)
        {
            var dic = new Dictionary<GameState, UIGameStateObject>();

            foreach (var ui in stateObjects)
                foreach (var s in ui.States)
                    dic[s] = ui;

            return dic;
        }

    }
}
