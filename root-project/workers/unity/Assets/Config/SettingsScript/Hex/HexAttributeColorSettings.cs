using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Strategy Config/HexAttributeColorSettings", order = 1)]
    public class HexAttributeColorSettings : BaseColorSettings
    {
        [SerializeField]
        HexAttributeColor[] hexAttributeColors;

        Type hexType = null;

        public UnityEngine.Color GetHexAttributeColor(HexAttribute attribute)
        {
            hexType = hexType ?? typeof(HexAttribute);
            var list = GetColorList(hexType);
            list = list ?? ConvertToColorList(hexType, hexAttributeColors);
            return GetColor(list, (uint)attribute);
        }
    }

    [Serializable]
    internal class HexAttributeColor : BaseColor, IColor
    {
        public uint Key => (uint)attribute;

        [SerializeField]
        HexAttribute attribute;
    }
}
