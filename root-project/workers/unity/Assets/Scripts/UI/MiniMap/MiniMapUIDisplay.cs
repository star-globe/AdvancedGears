using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Assertions;
using TMPro;

namespace AdvancedGears.UI
{
    public class MiniMapUIDisplay : SingletonMonobehaviour<MiniMapUIDisplay>
    {
        [SerializeField]
        Image raderImage;

        float raderRadius = -1.0f;
        float RaderRadius
        {
            get
            {
                if (raderRadius < 0) {
                    var rect = raderImage.GetComponent<RectTransform>();
                    if (rect == null)
                        raderRadius = 10.0f;
                    else
                        raderRadius = (rect.rect.width + rect.rect.height)/2;
                }
                
                return raderRadius;
            }
        }

        private float MiniMapRange
        {
            get { return RangeDictionary.MiniMapRange; }
        }

        public float MiniMapRate
        {
            get { return RaderRadius / MiniMapRange; }
        }

        public Transform MiniMapParent => raderImage.transform;

        public Vector2 GetMiniMapPos(Vector2 pos)
        {
            return pos * MiniMapRate;
        }

        private void Awake()
        {
            Assert.IsNotNull(raderImage);
            Initialize();
        }
    }
}
