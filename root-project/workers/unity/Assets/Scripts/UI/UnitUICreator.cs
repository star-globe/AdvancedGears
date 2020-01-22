using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears.UI
{
    public class UnitUICreator : MonoBehaviour
    {
        UnitHeadUI baseHeadUI = null;
        UnitHeadUI BaseHeadUI
        {
            get {
                if (baseHeadUI == null) {
                    var uiObject = UIObjectDictionary.GetUIObject(UIType.HeadStatus);
                    if (uiObject != null)
                        baseHeadUI = uiObject.GetComponent<UnitHeadUI>();
                }

                return baseHeadUI;
            }
        }

        readonly Dictionary<EntityId,UnitHeadUI> headUIDic = new Dictionary<EntityId, UnitHeadUI>();
        readonly Queue<UnitHeadUI> sleepUIList = new Queue<UnitHeadUI>();

        public UnitHeadUI GetOrCreateHeadUI(EntityId id)
        {
            if (headUIDic.ContainsKey(id))
                return headUIDic[id];

            UnitHeadUI ui = null;
            if (sleepUIList.Count > 0) {
                ui = sleepUIList.Dequeue();
                ui.gameObject.SetActive(true);
            }
            else {
                var go = Instantiate(this.BaseHeadUI.gameObject, Canvas.rootCanvas.transform);
                ui = go.GetComponent<UnitHeadUI>();
            }

            if (ui != null)
                headUIDic[id] = ui;

            return ui;
        }

        public void SleepUI(EntityId id)
        {
            if (headUIDic.ContainsKey(id) == false)
                return;

            sleepUIList.Enqueue(headUIDic[id]);
            headUIDic.Remove(id);
        }
    }
}
