using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears
{
    [RequireComponent(typeof(TextMeshPro))]
    public class HexInfoText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro text;

        public void SetHexInfo(uint index, int hexId, UnitSide side, Dictionary<UnitSide, float> powers)
        {
            if (text == null)
                return;

            var str = string.Format("Index:{0} HexId:{1}", index, hexId);
            foreach (var kvp in powers) {
                str += Environment.NewLine;
                str += string.Format("{0}:Pow:{1:0.00}", kvp.Key, kvp.Value);
            }
            text.SetText(str);
            text.color = ColorDictionary.GetSideColor(side);
        }
    }
}
