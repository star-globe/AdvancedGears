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

                if (unit) {
                    unit.side = side;

                    var type = UnitType.None;
                    switch (attribute)
                    {
                        case HexAttribute.Field:
                        case HexAttribute.ForwardBase:
                            type = UnitType.Stronghold;
                            break;

                        case HexAttribute.CentralBase:
                            type = UnitType.HeadQuarter;
                            break;
                    }

                    unit.type = type;
                    unit.gameObject.SetActive(type != UnitType.None);
                }
            }
        }

        [SerializeField]
        HexAttribute attribute;

        [SerializeField]
        UnitSide side;

        [SerializeField]
        int masterId;

        [SerializeField]
        UnitSnapshotPair pair;

        [SerializeField]
        LineRenderer line;

        [SerializeField]
        uint index = 0;
        public uint Index => index;

        public void SetPosition(Vector3 pos, uint index, float edge)
        {
            this.index = index;
            this.transform.position = pos;

            var corners = new Vector3[7];
            HexUtils.SetHexCorners(pos, corners, edge);
            line.positionCount = 7;
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
                pos = pos,
                side = side,
            };
        }

        public void SyncUnitSide()
        {
            pair.SetHexInfo(this.side, this.index, this.attribute);
        }

        public void SearchChildren()
        {
            var list = new List<UnitSnapshotPair>();
            var u = GetComponentInChildren<UnitSnapshotComponent>();
            if (u)
                this.pair = new UnitSnapshotPair() { unit = u, hex = u.GetComponent<HexSnapshotAttachment>() };
        }
    }

    [Serializable]
    public struct HexSnapshot
    {
        public uint index;
        public HexAttribute attribute;
        public int hexId;
        public Vector3 pos;
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
