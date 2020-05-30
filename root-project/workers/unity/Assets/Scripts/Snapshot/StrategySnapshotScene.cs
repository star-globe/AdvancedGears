using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    public class StrategySnapshotScene : SnapshotScene
    {
        [SerializeField]
        HexSnapshotComponent baseHexObject;

        [SerializeField]
        List<HexSnapshot> hexes = null;
        public List<HexSnapshot> Hexes => hexes;

        [SerializeField]
        HexDictionary hexDictionary;
        float edgeLength => hexDictionary.EdgeLength;

        readonly List<HexSnapshotComponent> realizeList = new List<HexSnapshotComponent>();
        readonly Queue<HexSnapshotComponent> sleepQueue = new Queue<HexSnapshotComponent>();

        IEnumerable<HexSnapshotComponent> GetHexBaseObjects(int number)
        {
            for (int i = 0; i < number; i++)
            {
                if (i >= realizeList.Count)
                {
                    if (sleepQueue.Count > 0) {
                        var hex = sleepQueue.Dequeue();
                        hex.gameObject.SetActive(true);
                        realizeList.Add(sleepQueue.Dequeue());
                    }
                    else
                        realizeList.Add(Instantiate(baseHexObject));
                }

                yield return realizeList[i];
            }

            for(int i = number; i < realizeList.Count; i++)
            {
                var hex = realizeList[i];
                hex.gameObject.SetActive(false);
                sleepQueue.Enqueue(hex);
            }
        }

        public void AlignHexes()
        {
            int number = Mathf.CeilToInt(WorldSize / edgeLength);
            number *= number;
            uint index = 0;

            var edge = edgeLength / rate;

            foreach (var h in GetHexBaseObjects(number)) {
                var center = HexUtils.GetHexCenter(this.transform.position, index, edge);
                h.SetPosition(center,index, edge);
                index++;
            }
        }

        public void ConvertHexes()
        {
            hexes.Clear();
            foreach (var u in realizeList)
                hexes.Add(u.GetHexSnapshot(rate, rate));
        }

        public override Snapshot GenerateSnapshot()
        {
            var snapshot = base.GenerateSnapshot();

            foreach (var h in Hexes) {
                var template = HexTemplate.CreateHexEntityTemplate(h.pos.ToCoordinates(), h.index, h.attribute, h.hexId, h.side);
                snapshot.AddEntity(template);
            }

            return snapshot;
        }
    }
}
