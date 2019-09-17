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

        private void Start()
        {
            outputPath = SnapshotGenerator.DefaultSnapshotPath;
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
