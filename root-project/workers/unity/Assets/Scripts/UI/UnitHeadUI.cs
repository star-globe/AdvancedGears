using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AdvancedGears.UI
{
    public class UnitHeadUI : BaseUIObject
    {
        [SerializeField]
        Vector3 offset;
        public Vector3 Offset => offset;

        [SerializeField]
        TextMeshProUGUI hpText;

        [SerializeField]
        Image lockOnImage;

        const string fmt = "{0}/{1}";
        int hp = -1;
        int maxHp = -1;

        public void SetInfo(Vector2 pos, int hp, int maxHp, bool isLockOn)
        {
            if (this.Rect != null)
                this.Rect.position = pos;

            if (this.hp != hp || this.maxHp != maxHp) {
                this.hp = hp;
                this.maxHp = maxHp;
                hpText?.SetText(string.Format(fmt, hp, maxHp));
            }

            if (lockOnImage != null && lockOnImage.gameObject.activeSelf != isLockOn)
                lockOnImage.gameObject.SetActive(isLockOn);
        }
    }

    public abstract class BaseUIObject : MonoBehaviour,IUIObject
    {
        RectTransform rect = null;
        public RectTransform Rect
        {
            get
            {
                rect = rect ?? GetComponent<RectTransform>();
                return rect;
            }
        }

        public virtual void Sleep()
        {
            this.gameObject.SetActive(false);
        }

        public virtual void WakeUp()
        {
            this.gameObject.SetActive(true);
        }
    }
}
