using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public delegate void CoalVanishEvent(long entityId, ulong coalId);

    public class CoalCreator : MonoBehaviour
    {
        class CoalsContainer
        {
            public CoalsContainer(uint type, Transform parent, Vector3 origin)
            {
                this.typeId = type;
                coalParent = parent;
                this.Origin = origin;
            }

            Vector3 Origin;
            Transform coalParent; 
            public uint typeId { get; private set;}

            GameObject coalObject = null;
            GameObject CoalObject
            {
                get
                {
                    if (coalObject == null) {
                        var settings = CoalDictionary.Get(this.typeId);
                        if (settings != null)
                            coalObject = settings.CoalModel;
                    }

                    return coalObject;
                }
            }

            readonly Queue<CoalInfoObject> deactiveQueue = new Queue<CoalInfoObject>();
            readonly Dictionary<long, Dictionary<ulong, CoalInfoObject>> coalsDic = new Dictionary<long, Dictionary<ulong, CoalInfoObject>>();
            readonly List<ulong> removeKeyList = new List<ulong>();

            public void Update()
            {
                foreach (var dic in coalsDic) {
                    removeKeyList.Clear();
                    foreach (var kvp in dic.Value) {
                        if (kvp.Value.IsActive == false)
                            removeKeyList.Add(kvp.Key);
                    }

                    foreach(var r in removeKeyList) {
                        deactiveQueue.Enqueue(dic.Value[r]);
                        dic.Value.Remove(r);
                    }
                }
            }

            public void OnCreate(AddCoalInfo add, long entityId, ulong coalId)
            {
                // check
                CoalInfoObject coal;
                if (deactiveQueue.Count > 1) {
                    coal = deactiveQueue.Dequeue();
                }
                else {
                    var go = Instantiate(this.CoalObject, this.coalParent);
                    coal = go.GetComponent<CoalInfoObject>();
                    coal.SetCallback(OnVanish);
                }

                coal.IsActive = true;
                coal.SetCoal(add.Amount, entityId, coalId);
                coal.transform.position = add.Pos.ToUnityVector() + this.Origin;

                // add
                var key = entityId;
                var id = coalId;
                if (coalsDic.ContainsKey(key)) {
                    var dic = coalsDic[key];
                    if (dic.ContainsKey(id))
                        dic[id] = coal;
                    else
                        dic.Add(id, coal);
                }
                else {
                    var dic = new Dictionary<ulong, CoalInfoObject>();
                    dic.Add(id, coal);
                    coalsDic.Add(key, dic);
                }
            }

            public void OnVanish(long entityId, ulong coalId)
            {
                if (coalsDic.TryGetValue(entityId, out var dic) == false)
                    return;

                if (dic.TryGetValue(coalId, out var coal) == false)
                    return;

                coal.IsActive = false;
            }
        }

        Dictionary<uint,CoalsContainer> containerDic = new Dictionary<uint, CoalsContainer>();

        Vector3 Origin;
        float checkTime = 0.0f;
        const float interval = 3.0f;

        public void Setup(Vector3 origin)
        {
            this.Origin = origin;
        }

        private void Update()
        {
            var current = Time.time;
            if (current - checkTime < interval)
                return;

            checkTime = current + interval;

            foreach (var kvp in containerDic)
                kvp.Value.Update();
        }

        public void OnCreateCoal(AddCoalInfo info, long entityId, ulong coalId)
        {
            CoalsContainer container;
            var type = info.Type;
            if (containerDic.TryGetValue(type, out container) == false) {
                container = new CoalsContainer(type, this.transform,  this.Origin);
                containerDic.Add(type, container);
            }

            container.OnCreate(info, entityId, coalId);
        }
    }
}
