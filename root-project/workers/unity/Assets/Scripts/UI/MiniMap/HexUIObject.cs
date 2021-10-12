using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears.UI
{
    public class HexUIObject : BaseUIObject
    {
        [SerializeField]
        Image image;

        private void Awake()
        {
            Assert.IsNotNull(image);
        }

        const float hexRate = 2.0f;

        public void SetInfo(Vector2 pos, UnitSide side, float rot, float hexSize)
        {
            if (this.Rect != null) {
                this.Rect.localPosition = pos;
                this.Rect.localEulerAngles = Vector3.forward * rot;
                this.Rect.sizeDelta = Vector2.one * hexSize * hexRate;
            }
                

            image.color = UIObjectDictionary.GetSideColor(side);
        }

        public override void Sleep()
        {
            base.Sleep();
        }
    }
}
