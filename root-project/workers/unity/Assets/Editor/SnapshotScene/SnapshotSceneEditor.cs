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

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI ();

            if (GUILayout.Button("Set Default Path") || outputPath == null)
            {
                outputPath = SnapshotGenerator.DefaultSnapshotRelativePath;
            }

            outputPath = EditorGUILayout.TextField(outputPath);

            if (GUILayout.Button("Show Test Field"))
            {
                scene.ShowTestField();
            }

            if (GUILayout.Button("Search and Convert"))
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
                OutputPath = SnapshotGenerator.GetDefaultSnapshotPath(outputPath)
            };

            SnapshotGenerator.Generate(arguments, scene.WorldSize, scene.GetHeight, scene.Units, scene.Fields);
        }
    }
}
