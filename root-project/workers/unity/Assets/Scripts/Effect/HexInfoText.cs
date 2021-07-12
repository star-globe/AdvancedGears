using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears.UI
{
    [RequireComponent(typeof(TextMeshPro))]
    public class HexInfoText : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro text;

        private StringBuilder builder = new StringBuilder();

        const string basicFmt = "Index:{0} HexId:{1}";
        const string powerFmt = ":Pow:{0:0.0}";

        uint index = uint.MaxValue;
        int hexId = int.MaxValue;

        string header = null;
        string GetHeader(uint i, int h)
        {
            if (this.index != i || this.hexId != h) {
                this.index = i;
                this.hexId = h;
                header = string.Format(basicFmt, this.index, this.hexId);
            }

            return header;
        }


        public void SetHexInfo(uint index, int hexId, UnitSide side, Dictionary<UnitSide, float> powers)
        {
            if (text == null)
                return;

            builder.Clear();
            builder.Append(GetHeader(index, hexId));
            foreach (var kvp in powers) {
                builder.AppendLine()
                       .Append(kvp.Key.GetName())
                       .AppendFormat(powerFmt, kvp.Value);
            }
            text.SetText(builder);
            text.color = ColorDictionary.GetSideColor(side);
        }
    }
}
