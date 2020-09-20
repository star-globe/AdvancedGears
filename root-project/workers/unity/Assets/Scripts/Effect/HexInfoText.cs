using System.Linq;
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

        public void SetHexInfo(uint index, int hexId, UnitSide side)
        {
            if (text == null)
                return;

            text.SetText(string.Format("Index:{0} HexId:{1}", index, hexId));
            text.color = ColorDictionary.GetSideColor(side);
        }
    }
}
