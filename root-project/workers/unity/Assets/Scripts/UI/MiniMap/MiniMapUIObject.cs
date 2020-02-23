using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears.UI
{
    public class MiniMapUIObject : BaseUIObject
    {
        [SerializeField]
        Image image;

        [SerializeField]
        TextMeshProUGUI playerNameText;

        private void Awake()
        {
            Assert.IsNotNull(image);
            Assert.IsNotNull(playerNameText);
        }

        public void SetInfo(Vector2 pos, UnitSide side, UnitType type)
        {
            if (this.Rect != null)
                this.Rect.position = pos;

            image.color = UIObjectDictionary.GetSideColor(side);
            image.sprite = UIObjectDictionary.GetUnitSprite(type);
        }

        public void SetName(string name)
        {
            playerNameText.gameObject.SetActive(string.IsNullOrEmpty(name) == false);
            playerNameText.SetText(name);
        }

        public override void Sleep()
        {
            base.Sleep();
            SetName(string.Empty);
        }
    }
}
