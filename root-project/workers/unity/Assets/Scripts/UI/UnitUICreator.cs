using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable;
using Improbable.Gdk.Subscriptions;
using AdvancedGears;

namespace AdvancedGears.UI
{
    public class UnitUICreator : MonoBehaviour
    {
        [Require] World world;
 
        [SerializeField]
        Canvas canvas;

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

        private void Start()
        {
            var system = world.GetExistingSystem<UnitUIInfoSystem>();
            if (system != null)
                system.UnitUICreator = this;
        }

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
                var go = Instantiate(this.BaseHeadUI.gameObject, canvas.transform);
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
