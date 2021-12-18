using System;
using Improbable.Gdk.TransformSynchronization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
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
        List<SymbolicTowerSnapshot> towers = null;
        public List<SymbolicTowerSnapshot> Towers => towers;

        [SerializeField]
        FieldDictionary dictionary;
        float WorldHeight => dictionary.MaxHeight;

        [SerializeField]
        UnitCommonSettingsDictionary unitDictionary;

        Vector3 size => terrain.terrainData.size;

        public float rate => this.WorldSize / localSize;

        public Vector3 CenterPos => this.transform.position + new Vector3(localSize / 2, 0, localSize / 2);

        private void Start()
        {
            Initialize();
        }

        private bool IsBuilding(UnitType type)
        {
            var set = unitDictionary?.GetUnitSettings(type);
            if (set == null)
                return false;

            return set.isBuilding;
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

            towers.Clear();
            foreach (var t in FindObjectsOfType<SymbolicTowerSnapshotComponent>())
                towers.Add(t.GetSymbolicTowerSnapshot(rate, rate));
        }

        public void ShowTestField()
        {
            var list = new List<ValueTuple<List<TerrainPointInfo>,Vector3>>();
            foreach (var f in FindObjectsOfType<FieldSnapshotComponent>())
            {
                list.Add((FieldTemplate.CreateTerrainPointInfo(f.Range, f.Highest, f.MaterialType, f.Seeds), f.transform.position));
            }

            realizer.ResetField();
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

        public virtual Snapshot GenerateSnapshot()
        {
            var snapshot = SnapshotUtils.GenerateGroundSnapshot(this.WorldSize, this.GetHeight);

            foreach (var t in Towers)
                snapshot.AddEntity(SymbolicTowerTemplate.CreateSymbolicTowerEntityTemplate(t.pos.ToCoordinates(), t.height, t.radius, t.side));

            foreach(var f in Fields)
                snapshot.AddEntity(FieldTemplate.CreateFieldEntityTemplate(f.pos.ToCoordinates(), f.range, f.highest, f.materialType, f.seeds));

            foreach(var u in Units) {
                var template = BaseUnitTemplate.CreateBaseUnitEntityTemplate(u.side, u.type, SnapshotUtils.GroundCoordinates(u.pos.x, u.pos.y, u.pos.z, this.IsBuilding(u.type) == false), u.rotate.ToCompressedQuaternion());
                if (u.attachments != null) {
                    foreach (var attach in u.attachments) {
                        attach.AddComponent(template);
                    }
                }

                snapshot.AddEntity(template);
            }

            SnapshotUtils.AddLongRangeBulletReciever(this.WorldSize, snapshot);

            return snapshot;
        }
    }
}
