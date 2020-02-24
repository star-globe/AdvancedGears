using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
        interface IUIContainer
        {
            void ResetAll();
            void SleepAllUnused();
            Component GetOrCreateUI(EntityId id, Transform parent);
        }

        class UIContainer<T> : IUIContainer where T : Component, IUIObject
        {
            public UIType uiType { get; private set; }

            public UIContainer (UIType uiType)
            {
                this.uiType = uiType;
            }

            T baseUI = null;
            T BaseUI
            {
                get
                {
                    if (baseUI == null)
                    {
                        var uiObject = UIObjectDictionary.GetUIObject(uiType);
                        if (uiObject != null)
                            baseUI = uiObject.GetComponent<T>();
                    }

                    return baseUI;
                }
            }

            readonly Dictionary<EntityId, UIChache<T>> uiDic = new Dictionary<EntityId, UIChache<T>>();
            readonly Queue<T> sleepUIList = new Queue<T>();

            public void ResetAll()
            {
                foreach (var kvp in uiDic)
                    kvp.Value.Reset();
            }

            HashSet<EntityId> ids = new HashSet<EntityId>();

            public void SleepAllUnused()
            {
                ids.Clear();
                foreach (var kvp in uiDic)
                {
                    if (kvp.Value.isChecked == false)
                        ids.Add(kvp.Key);
                }

                foreach (var i in ids)
                    SleepUI(i);
            }

            public void SleepUI(EntityId id)
            {
                if (uiDic.ContainsKey(id) == false)
                    return;

                var ui = uiDic[id].GetUI();
                ui.Sleep();
                sleepUIList.Enqueue(ui);
                uiDic.Remove(id);
            }

            public Component GetOrCreateUI(EntityId id, Transform parent)
            {
                if (uiDic.ContainsKey(id))
                    return uiDic[id].GetUI();

                T ui = null;
                if (sleepUIList.Count > 0)
                {
                    ui = sleepUIList.Dequeue();
                    ui.WakeUp();
                }
                else if(this.BaseUI != null)
                {
                    var go = Instantiate(this.BaseUI.gameObject, parent);
                    ui = go.GetComponent<T>();
                }

                if (ui != null)
                    uiDic[id] = new UIChache<T>(ui);

                return ui;
            }
        }

        class UIChache<T> where T : Component
        {
            T ui;
            public bool isChecked { get; private set; }

            public UIChache(T ui)
            {
                this.ui = ui;
                isChecked = true;
            }

            public void Reset()
            {
                isChecked = false;
            }

            public T GetUI()
            {
                isChecked = true;
                return this.ui;
            }
        }

        [SerializeField]
        Canvas canvas;

        private void Awake()
        {
            Assert.IsNotNull(canvas);
            Initialize();
        }

        readonly Dictionary<UIType, IUIContainer> containersDic = new Dictionary<UIType, IUIContainer>();

        IUIContainer GetContainer(UIType type)
        {
            if (containersDic.ContainsKey(type))
                return containersDic[type];
            else
                return null;
        }

        public bool ContainsType(UIType type)
        {
            return containersDic.ContainsKey(type);
        }

        public void AddContainer<T>(UIType uiType) where T : Component,IUIObject
        {
            containersDic.Add(uiType, new UIContainer<T>(uiType));
        }

        public void ResetAll(params UIType [] types)
        {
            foreach (var t in types) {
                GetContainer(t)?.ResetAll();
            }
        }

        public void SleepAllUnused(params UIType[] types)
        {
            foreach (var t in types) {
                GetContainer(t)?.SleepAllUnused();
            }
        }

        public Component GetOrCreateUI(UIType type, EntityId id, Transform parent = null)
        {
            parent = parent ?? canvas.transform;
            return GetContainer(type)?.GetOrCreateUI(id, parent);
        }
    }

    public interface IUIObject
    {
        void WakeUp();
        void Sleep();
    }
}
