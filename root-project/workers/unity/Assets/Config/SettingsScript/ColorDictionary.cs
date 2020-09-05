using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/Color Dictionary", order = 0)]
    public class ColorDictionary : DictionarySettings
    {
        public static ColorDictionary Instance { private get; set; }

        [SerializeField] private BaseUnitStateColorSettings baseUnitStateColorSettings;

        public override void Initialize()
        {
            Instance = this;
        }

        public static UnityEngine.Color GetSideColor(UnitSide side)
        {
            return Instance.baseUnitStateColorSettings.GetSideColor(side);
        }

        public static UnityEngine.Color GetStateColor(UnitState state)
        {
            return Instance.baseUnitStateColorSettings.GetStateColor(state);
        }
    }
}
