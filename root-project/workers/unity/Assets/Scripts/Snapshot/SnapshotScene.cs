using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class SnapshotScene : MonoBehaviour
    {
        [SerializeField, Tooltip("holizon, vertical")]
        Vector2 worldSize;
        public Vector2 WorldSize => worldSize;

        [SerializeField]
        Terrain terrain;

        [SerializeField]
        List<UnitSnapshot> units = null;
        public List<UnitSnapshot> Units => units;

        [SerializeField]
        List<FieldSnapshot> fields = null;
        public List<FieldSnapshot> Fields => fields;

        public void SearchAndConvert()
        {
            var size = terrain.terrainData.size;
            float rateHolizon = worldSize.x / size.x;
            float rateVertical = worldSize.y / size.y;

            foreach (var u in FindObjectsOfType<UnitSnapshotComponent>())
                units.Add(u.GetUnitSnapshot(rateHolizon, rateVertical));

            fields.Clear();
            foreach (var f in FindObjectsOfType<FieldSnapshotComponent>())
                fields.Add(f.GetFieldSnapshot(rateHolizon,rateVertical));
        }
    }

    public struct UnitSnapshot
    {
        public UnitType type;
        public UnitSide side;
        public Vector3 pos;
    }

    public struct FieldSnapshot
    {
        public float highest;
        public float range;
        public FieldMaterialType materialType;
        public Vector3 pos;
    }
}
