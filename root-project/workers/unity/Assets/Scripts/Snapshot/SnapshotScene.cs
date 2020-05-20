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
        float localSize;

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

        public float rate => this.WorldSize / localSize;

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            this.transform.position = new Vector3(-localSize/2,0,-localSize/2);
            realizer.Setup(localSize, dictionary);
        }

        public void SearchAndConvert()
        {
            units.Clear();
            foreach (var u in FindObjectsOfType<UnitSnapshotComponent>())
                units.Add(u.GetUnitSnapshot(rate, rate));

            fields.Clear();
            foreach (var f in FindObjectsOfType<FieldSnapshotComponent>())
                fields.Add(f.GetFieldSnapshot(rate, rate, dictionary.MaxRange));
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
                u.SetHeight(rate);
        }

        public float GetHeight(float x, float z)
        {
            var point = PhysicsUtils.GetGroundPosition(new Vector3(x / rate, 1000.0f, z / rate));
            return point.y * rate;
        }

        public virtual void GenerateSnapshot(string outputPath)
        {
            var snapshot = SnapshotGenerator.GenerateGroundSnapshot(this.WorldSize, this.GetHeight);
            
        }
    }
}
