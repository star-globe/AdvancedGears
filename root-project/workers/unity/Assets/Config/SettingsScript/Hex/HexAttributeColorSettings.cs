using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Strategy Config/HexAttributeColorSettings", order = 1)]
    public class HexAttributeColorSettings : ScriptableObject
    {
        [SerializeField]
        HexAttributeColor[] hexAttributeColors;

        public UnityEngine.Color GetAttributeColor(HexAttribute attribute)
        {
            return GetColor(hexAttributeColors,attribute);
        }
    }

    [Serializable]
    internal class HexAttributeColor : BaseColor, IColor<HexAttribute>
    {
        public HexAttribute Tgt => attribute;

        [SerializeField]
        HexAttribute attribute;
    }
}
