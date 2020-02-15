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
    public class UnitUICreator : SingletonMonobehaviour<UnitUICreator>
    {
        class UnitHeadUIChache
        {
            UnitHeadUI ui;
            public bool isChecked { get; private set; }

            public UnitHeadUIChache(UnitHeadUI ui)
            {
                this.ui = ui;
                isChecked = true;
            }

            public void Reset()
            {
                isChecked = false;
            }

            public UnitHeadUI GetUI()
            {
                isChecked = true;
                return this.ui;
            }
        }

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

        readonly Dictionary<EntityId,UnitHeadUIChache> headUIDic = new Dictionary<EntityId, UnitHeadUIChache>();
        readonly Queue<UnitHeadUI> sleepUIList = new Queue<UnitHeadUI>();

        public void ResetAll()
        {
            foreach(var kvp in headUIDic)
                kvp.Value.Reset();
        }

        HashSet<EntityId> ids = new HashSet<EntityId>();

        public void SleepAllUnused()
        {
            ids.Clear();
            foreach(var kvp in headUIDic) {
                if (kvp.Value.isChecked == false)
                    ids.Add(kvp.Key);
            }

            foreach(var i in ids)
                SleepUI(i);

            foreach (var u in sleepUIList)
                u.gameObject.SetActive(false);
        }

        public UnitHeadUI GetOrCreateHeadUI(EntityId id)
        {
            if (headUIDic.ContainsKey(id))
                return headUIDic[id].GetUI();

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
                headUIDic[id] = new UnitHeadUIChache(ui);

            return ui;
        }

        public void SleepUI(EntityId id)
        {
            if (headUIDic.ContainsKey(id) == false)
                return;

            var ui = headUIDic[id].GetUI();
            ui.gameObject.SetActive(false);
            sleepUIList.Enqueue(ui);
            headUIDic.Remove(id);
        }
    }
}
