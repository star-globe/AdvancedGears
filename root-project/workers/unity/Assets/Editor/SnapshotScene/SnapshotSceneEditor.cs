using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    [CustomEditor (typeof(SnapshotScene))]
    public class SnapshotSceneEditor : UnityEditor.Editor
    {
        SnapshotScene scene = null;

        string outputPath = null;

        void OnEnable ()
        {
            scene = target as SnapshotScene;
        }

        Rect rect = new Rect(10, 25, 200 - 20, 20);

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI ();

            if (GUILayout.Button("Set Default Path") || outputPath == null)
            {
                outputPath = SnapshotGenerator.DefaultSnapshotPath;
            }

            outputPath = EditorGUILayout.TextField(outputPath);

            if(GUILayout.Button("Search and Convert"))
            {
                scene.SearchAndConvert();
            }

            if(GUILayout.Button("Generate Snapshot"))
            {
                GenerateSnapshot();
            }
        }

        void GenerateSnapshot()
        {
            if (scene == null)
                return;

            var arguments = new SnapshotGenerator.Arguments
            {
                OutputPath = SnapshotGenerator.DefaultSnapshotPath
            };

            SnapshotGenerator.Generate(arguments, scene.WorldSize.x, null, scene.Units, scene.Fields);
        }
    }
}
