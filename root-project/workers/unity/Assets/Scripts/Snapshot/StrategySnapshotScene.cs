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

        IEnumerable<HexSnapshotComponent> GetHexBaseObjects(int number)
        {
            number = 3 * number * (number+1) + 1;

            var comps = FindObjectsOfType<HexSnapshotComponent>();
            Dictionary<uint, HexSnapshotComponent> hexDic = new Dictionary<uint, HexSnapshotComponent>();

            foreach (var c in comps) {
                hexDic[c.Index] = c;
            }

            for (uint i = 0; i < number; i++)
            {
                HexSnapshotComponent hex = null;
                if (hexDic.ContainsKey(i)) {
                    hex = hexDic[i];
                    hex.gameObject.SetActive(true);
                    hexDic.Remove(i);
                }
                else {
                    hex = Instantiate(baseHexObject);
                    hex.gameObject.name = string.Format("Hex_ID:{0}", i);
                }

                yield return hex;
            }

            foreach(var kvp in hexDic) {
                kvp.Value.gameObject.SetActive(false);
            }
        }

        public void AlignHexes()
        {
            var centerPos = this.CenterPos;
            int edgeNumber = Mathf.CeilToInt((WorldSize / 2 / edgeLength - 1.0f) / 2);
            uint index = 0;

            var edge = edgeLength / rate;

            foreach (var h in GetHexBaseObjects(edgeNumber)) {
                var center = HexUtils.GetHexCenter(centerPos, index, edge);
                h.SetPosition(center,index, edge);
                index++;
            }
        }

        public void ConvertHexes()
        {
            hexes.Clear();
            foreach (var u in FindObjectsOfType<HexSnapshotComponent>()) {
                if (u.gameObject.activeSelf)
                    hexes.Add(u.GetHexSnapshot(rate, rate));
            }
        }

        //public override Snapshot GenerateSnapshot()
        //{
        //    var snapshot = base.GenerateSnapshot();
        //
        //    foreach (var h in Hexes) {
        //        var template = HexTemplate.CreateHexEntityTemplate(h.pos.ToCoordinates(), h.index, h.attribute, h.hexId, h.side);
        //        snapshot.AddEntity(template);
        //    }
        //
        //    return snapshot;
        //}
    }
}
