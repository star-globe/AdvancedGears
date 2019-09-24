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
        float worldSize;
        public float WorldSize => worldSize;

        [SerializeField]
        Terrain terrain;

        [SerializeField]
        FieldRealizer realizer;

        [SerializeField]
        List<UnitSnapshot> units = null;
        public List<UnitSnapshot> Units => units;

        [SerializeField]
        List<FieldSnapshot> fields = null;
        public List<FieldSnapshot> Fields => fields;

        [SerializeField]
        FieldDictionary dictionary;
        float WorldHeight => dictionary.MaxHeight;

        Vector3 size => terrain.terrainData.size;

        float rateHolizon => this.WorldSize / size.x;
        float rateVertical => this.WorldHeight / size.y;

        public void SearchAndConvert()
        {
            units.Clear();
            foreach (var u in FindObjectsOfType<UnitSnapshotComponent>())
                units.Add(u.GetUnitSnapshot(rateHolizon, rateVertical));

            fields.Clear();
            foreach (var f in FindObjectsOfType<FieldSnapshotComponent>())
                fields.Add(f.GetFieldSnapshot(rateHolizon, rateVertical, dictionary.MaxRange));
        }

        public void ShowTestField()
        {
            var list = new List<ValueTuple<List<TerrainPointInfo>,Vector3>>();
            foreach (var f in FindObjectsOfType<FieldSnapshotComponent>())
            {
                list.Add((FieldTemplate.CreateTerrainPointInfo(f.Range, f.Highest, f.MaterialType, f.Seeds), f.transform.position));
            }

            realizer.Reset();
            foreach (var tuple in list)
            {
                realizer.Realize(Vector3.zero, tuple.Item1, tuple.Item2);
            }

            foreach (var u in FindObjectsOfType<UnitSnapshotComponent>())
                u.SetHeight();
        }

        public float GetHeight(float x, float z)
        {
            var ray = new Ray(new Vector3(x / rateHolizon, 1000.0f, z/ rateHolizon), Vector3.down);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, LayerMask.GetMask("Ground"));

            return hit.point.y * rateVertical;
        }
    }

    [Serializable]
    public struct UnitSnapshot
    {
        public UnitType type;
        public UnitSide side;
        public Vector3 pos;
    }

    [Serializable]
    public struct FieldSnapshot
    {
        public float highest;
        public float range;
        public FieldMaterialType materialType;
        public Vector3 pos;
        public int seeds;
    }
}
