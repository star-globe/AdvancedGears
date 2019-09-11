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

        readonly Dictionary<int,Dictionary<int,FieldRealizer>> realizedDic = new Dictionary<int, Dictionary<int, FieldRealizer>>();
        readonly Queue<GameObject> objectQueue = new Queue<GameObject>();

        GameObject FieldObject
        {
            get
            {
                GameObject fieldObject;
                if (objectQueue.Count == 0) {
                    var settings = FieldDictionary.Get(0);
                    if (settings != null)
                        fieldObject = Instantiate(settings.FieldObject);
                }
                else {
                    fieldObject = objectQueue.Dequeue();
                }

                return fieldObject;
            }
        }

        StaticBulletReceiver staticReceiver = null;
        StaticBulletReceiver StaticReceiver
        {
            get
            {
                if (staticReceiver == null)
                {
                    staticReceiver = this.FieldObject.GetComponent<StaticBulletReceiver>();
                }
                return staticReceiver;
            }
        }

        FieldRealizer fieldRealizer = null;
        FieldRealizer FieldRealizer
        {
            get
            {
                if (fieldRealizer == null)
                {
                    fieldRealizer = this.FieldObject.GetComponent<FieldRealizer>();
                }
                return fieldRealizer;
            }
        }

        public void Setup(World world, Vector3 origin)
        {
            this.world = world;
            this.Origin = origin;
        }

        public void Reset()
        {
            foreach (var yDic in realizedDic) {
                foreach (var xKvp in yDic) {
                    xKvp.Value.Reset();
                }
            }
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
            this.StaticReceiver.SetWorld(world);
            var pos = center != null ? center.Value: this.Origin;
            GetRealizer(pos).Realize(terrainPoints, coords.ToUnityVector() + this.Origin, pos);
        }

        FieldRealizer GetRealizer(Vector3 pos)
        {
            var size = FieldRealizer.FieldSize;
            int x = (int)(pos.x / size);
            int y = (int)(pos.y / size);

            Dictionary<int,FieldRealizer> dic;
            if (realizedDic.ContainsKey(y))
                dic = realizedDic[y];
            else
                dic = new Dictionary<int, FieldRealizer>();

            FieldRealizer realizer;
            if (dic.ContainsKey(x))
                realizer = dic[x];
            else
                realizer = this.FieldRealizer;

            dic[x] = realizer;
            realizedDic[y] = dic;

            return realizer;
        }
    }
}

