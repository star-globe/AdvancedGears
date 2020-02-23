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
					raderRadius = rect == null ? 10.0f: rect.sizeDelta.magnitude;
                }
                
                return raderRadius;
            }
        }

        private float MiniMapRange
        {
            get { return RangeDictionary.MiniMapRange; }
        }

        public Transform MiniMapParent => raderImage.transform;

        public Vector2 GetMiniMapPos(Vector2 pos)
        {
            return (pos / MiniMapRange) * RaderRadius;
        }

        private void Awake()
        {
            Assert.IsNotNull(raderImage);
        }
    }
}
