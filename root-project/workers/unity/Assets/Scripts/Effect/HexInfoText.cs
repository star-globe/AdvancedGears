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

        public void SetHexInfo(uint index, int hexId, UnitSide side, float resource)
        {
            if (text == null)
                return;

            text.SetText(string.Format("Index:{0} HexId:{1} Resource:{{2:f2}}", index, hexId, resource));
            text.color = ColorDictionary.GetSideColor(side);
        }
    }
}
