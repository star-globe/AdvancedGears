using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class StrategySnapshotScene : SnapshotScene
    {
        [SerializeField]
        List<HexSnapshot> hexes = null;
        public List<HexSnapshot> Hexes => hexes;

        [SerializeField]
        HexDictionary dictionary;
        float edgeLength => dictionary.EdgeLength;

        public void AlignHexes()
        {
            int index = 0;
            foreach (var h in FindObjectsOfType<HexSnapshotComponent>()) {
                var center = HexUtils.GetHexCenter(this.transform.position, index, edgeLength);
                h.SetPosition(center,index);
                index++;
            }
        }

        public void ConvertHexes()
        {
            hexes.Clear();
            foreach (var u in FindObjectsOfType<HexSnapshotComponent>())
                hexes.Add(u.GetHexSnapshot(rate, rate));
        }
    }
}
