using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    public class SnapshotScene : MonoBehaviour
    {
        [SerializeField]
        string outputPath;

        [SerializeField, Tooltip("holizon, vertical")]
        Vector2 worldSize;

        [SerializeField]
        Terrain terrain;

        readonly List<UnitSnapshot> units = new List<UnitSnapshot>();
        readonly List<FieldSnapshot> fields = new List<FieldSnapshot>();

        private void Start()
        {
            outputPath = SnapshotGenerator.DefaultSnapshotPath;
        }

        public void SearchAndConvert()
        {
            var size = terrain.terrainData.size;
            float rateHolizon = worldSize.x / size.x;
            float rateVertical = worldSize.y / size.y;

            
        }

        public void GenerateSnapshot()
        {
            var arguments = new SnapshotGenerator.Arguments
            {
                OutputPath = SnapshotGenerator.DefaultSnapshotPath
            };

            SnapshotGenerator.Generate(arguments);
        }
    }

    #if UNITY_EDITOR
    [CustomEditor (typeof(SnapshotScene))]
    public class SnapshotSceneEditor : UnityEditor.Editor
    {
        SnapshotScene scene = null;

        void OnEnable ()
        {
            scene = target as SnapshotScene;
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI ();

            if(GUILayout.Button("Generate Snapshot"))
            {
                scene.GenerateSnapshot();
            }
        }
    }
    #endif
}
