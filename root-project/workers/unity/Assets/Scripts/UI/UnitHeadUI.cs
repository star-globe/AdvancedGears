using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears.UI
{
    public class UnitHeadUI : MonoBehaviour
    {
        [SerializeField]
        Vector3 offset;
        public Vector3 Offset => offset;

        [SerializeField]
        TextMeshProUGUI hpText;

        RectTransform rect = null;
        RectTransform Rect
        {
            get {
                rect = rect ?? GetComponent<RectTransform>();
                return rect;
            }
        }

        public void SetInfo(Vector2 pos, int hp)
        {
            if (this.Rect != null)
                this.Rect.position = pos;

            hpText?.SetText(hp.ToString());
        }
    }
}
