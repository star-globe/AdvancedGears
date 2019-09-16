using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class FieldCreator : MonoBehaviour
    {
        World world;
        Vector3 Origin;
        string WorkerId;
        FieldWorkerType workerType;

        readonly Dictionary<int,Dictionary<int,FieldRealizer>> realizedDic = new Dictionary<int, Dictionary<int, FieldRealizer>>();
        readonly Queue<GameObject> objectQueue = new Queue<GameObject>();

        public bool IsSetDatas { get; private set; }
        public FieldSettings Settings
        {
            get { return FieldDictionary.Get(workerType); }
        }

        GameObject GetNewFieldObject()
        {
            GameObject fieldObject = null;
            if (objectQueue.Count == 0) {
                var settings = this.Settings;
                if (settings != null) {
                    fieldObject = Instantiate(settings.FieldObject);
                    fieldObject.name += this.WorkerId;
                }
            }
            else {
                fieldObject = objectQueue.Dequeue();
            }

            return fieldObject;
        }

        FieldRealizer GetNewFieldRealizer()
        {
            var fieldObject = GetNewFieldObject();
            if (fieldObject == null)
                return null;

            var receiver = fieldObject.GetComponent<StaticBulletReceiver>();
            if (receiver != null)
                receiver.SetWorld(this.world);

            var realizer = fieldObject.GetComponent<FieldRealizer>();
            realizer.Setup(this.Settings.FieldSize);

            return realizer;
        }

        private void Awake()
        {
            IsSetDatas = false;
        }

        public void Setup(World world, Vector3 origin, string workerId, FieldWorkerType type)
        {
            this.world = world;
            this.Origin = origin;
            this.WorkerId = workerId;
            this.workerType = type;
        }

        public void Reset()
        {
            foreach (var yDic in realizedDic) {
                foreach (var xKvp in yDic.Value) {
                    xKvp.Value.Reset();
                }
            }

            IsSetDatas = false;
        }

        public void RemoveFields()
        {
            List<int> yList = new List<int>();
            foreach (var yDic in realizedDic) {
                List<int> xList = new List<int>();

                foreach (var xKvp in yDic.Value) {
                    if (xKvp.Value.IsSet == false)
                        xList.Add(xKvp.Key);
                }

                foreach (var key in xList) {
                    objectQueue.Enqueue(yDic.Value[key].gameObject);
                    yDic.Value.Remove(key);
                }

                if (yDic.Value.Count == 0)
                    yList.Add(yDic.Key);
            }

            foreach (var key in yList)
                realizedDic.Remove(key);
        }

        public void RealizeField(List<TerrainPointInfo> terrainPoints, Coordinates coords, Vector3? center = null)
        {
            var pos = center != null ? center.Value: this.Origin;
            GetRealizer(pos).Realize(pos, terrainPoints, coords.ToUnityVector() + this.Origin);
            IsSetDatas = true;
        }

        public void RealizeEmptyField(Vector3? center = null)
        {
            var pos = center != null ? center.Value: this.Origin;
            GetRealizer(pos).Realize(pos);
            IsSetDatas = true;
        }

        FieldRealizer GetRealizer(Vector3 pos)
        {
            var size = this.Settings.FieldSize;
            int x = (int)(pos.x / size);
            int y = (int)(pos.z / size);

            Dictionary<int,FieldRealizer> dic;
            if (realizedDic.ContainsKey(y))
                dic = realizedDic[y];
            else
                dic = new Dictionary<int, FieldRealizer>();

            FieldRealizer realizer;
            if (dic.ContainsKey(x))
                realizer = dic[x];
            else
                realizer = GetNewFieldRealizer();

            dic[x] = realizer;
            realizedDic[y] = dic;

            return realizer;
        }
    }
}

