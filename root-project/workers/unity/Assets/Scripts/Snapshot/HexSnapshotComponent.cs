using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Core;
using AdvancedGears;

namespace AdvancedGears
{
    public class HexSnapshotComponent : MonoBehaviour
    {
        [Serializable]
        class UnitSnapshotPair
        {
            public UnitSnapshotComponent unit;
            public HexSnapshotAttachment hex;

            public void SetHexInfo(UnitSide side, uint index, HexAttribute attribute)
            {
                if (hex) {
                    hex.hexIndex = index;
                    hex.attribute = attribute;
                }

                if (unit)
                    unit.side = side;
            }
        }

        [SerializeField]
        HexAttribute attribute;

        [SerializeField]
        UnitSide side;

        [SerializeField]
        int masterId;

        [SerializeField]
        UnitSnapshotPair[] pairs;

        [SerializeField]
        LineRenderer line;

        uint index = 0;

        public void SetPosition(Vector3 pos, uint index, float edge)
        {
            this.index = index;
            this.transform.position = pos;

            var corners = new Vector3[6];
            HexUtils.SetHexCorners(pos, corners, edge);
            line.SetPositions(corners);
        }

        public HexSnapshot GetHexSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new HexSnapshot()
            {
                index = index,
                attribute = attribute,
                hexId = masterId,
                side = side,
            };
        }

        public void SyncUnitSide()
        {
            foreach (var p in pairs)
                p.SetHexInfo(this.side, this.index, this.attribute);
        }

        public void SearchChildren()
        {
            var list = new List<UnitSnapshotPair>();
            var units = GetComponentsInChildren<UnitSnapshotComponent>();
            foreach(var u in units) {
                list.Add(new UnitSnapshotPair() { unit = u, hex = u.GetComponent<HexSnapshotAttachment>() });
            }

            this.pairs = list.ToArray();
        }
    }

    [Serializable]
    public struct HexSnapshot
    {
        public uint index;
        public HexAttribute attribute;
        public int hexId;
        public UnitSide side;
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(HexSnapshotComponent))]
    public class HexSnapshotComponentEditor : UnityEditor.Editor
    {
        HexSnapshotComponent component = null;

        void OnEnable()
        {
            component = target as HexSnapshotComponent;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("SyncUnitSide"))
                component.SyncUnitSide();

            if (GUILayout.Button("SearchChildren"))
                component.SearchChildren();
        }
    }
#endif
}
