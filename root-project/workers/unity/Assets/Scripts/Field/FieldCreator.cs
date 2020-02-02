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
        struct IndexXY
        {
            public int x;
            public int y;

            public IndexXY(int x, int y)
            {
                this.x = x;
                this.y = y;
            }

            public static bool CheckAndRenew(Vector3 pos, float size, ref IndexXY? xy)
            {
                int x = Mathf.FloorToInt((pos.x + size / 2) / size);
                int y = Mathf.FloorToInt((pos.z + size / 2) / size);

                if (xy == null || xy.Value.x != x || xy.Value.y != y)
                {
                    xy = new IndexXY(x, y);
                    return true;
                }

                return false;
            }
        }

        World world;
        Vector3 Origin;
        string WorkerId;
        FieldWorkerType workerType;

        IndexXY? indexXY = null;

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

        Vector3 getPos(Vector3? center = null)
        {
            if (center != null)
                return new Vector3(center.Value.x, this.Origin.y, center.Value.z);
            else
                return this.Origin;
        }

        public void RealizeField(List<TerrainPointInfo> terrainPoints, Coordinates coords, Vector3? center = null)
        {
            Debug.LogFormat("Coords:{0}", coords);

            Vector3 pos = getPos(center);
            var realizer = GetRealizer(pos, out var c);
            realizer.Realize(c, terrainPoints, coords.ToUnityVector() + this.Origin);
            IsSetDatas = true;
        }

        public void RealizeEmptyField(Vector3? center = null)
        {
            Vector3 pos = getPos(center);
            var realizer = GetRealizer(pos, out var c);
            realizer.Realize(c);
            IsSetDatas = true;
        }

        public bool CheckNeedRealize(Vector3 center)
        {
            if (indexXY == null)
                return true;

            Vector3 pos = getPos(center);
            return IndexXY.CheckAndRenew(pos, ChunkRange, ref indexXY);
        }

        private float ChunkRange => this.Settings.FieldSize * FieldDictionary.ChunkRangeRate;

        FieldRealizer GetRealizer(Vector3 pos, out Vector3 center)
        {
            IndexXY.CheckAndRenew(pos, ChunkRange, ref indexXY);

            var x = indexXY.Value.x;
            var y = indexXY.Value.y;

            DebugUtils.LogFormatColor(UnityEngine.Color.blue, "realize Indexies :[{0}][{1}] postion:{2}", x, y, pos);

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

            var size = ChunkRange;
            center = new Vector3(x * size, 0 , y * size) + this.Origin;

            return realizer;
        }
    }
}

