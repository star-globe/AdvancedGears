using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears
{
    public class HexGridComponent : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI numberText;

        [SerializeField]
        Image hexImage;

        [SerializeField]
        RectTransform myRect;
        public RectTransform MyRect => myRect;

        public void DrawGrid(uint index)
        {
            numberText.SetText(index.ToString());
        }
    }
}
