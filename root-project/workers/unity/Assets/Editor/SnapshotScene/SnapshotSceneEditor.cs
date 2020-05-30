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
        protected SnapshotScene scene = null;

        string outputPath = null;

        void OnEnable ()
        {
                AttachScene();
        }

        protected virtual void AttachScene()
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
            else if (GUILayout.Button("Set Cloud Path"))
            {
                outputPath = SnapshotGenerator.CloudSnapshotRelativePath;
            }

            outputPath = EditorGUILayout.TextField(outputPath);

            if (GUILayout.Button("Initialize"))
                scene.Initialize();

            if (GUILayout.Button("Show Test Field"))
                scene.ShowTestField();

            if (GUILayout.Button("Search and Convert"))
                scene.SearchAndConvert();

            if(GUILayout.Button("Generate Snapshot"))
                GenerateSnapshot();
        }

        protected virtual void GenerateSnapshot()
        {
            if (scene == null)
                return;

            var arguments = new SnapshotGenerator.Arguments
            {
                OutputPath = SnapshotGenerator.GetSnapshotPath(outputPath)
            };

            var snapshot = scene.GenerateSnapshot();
            Debug.Log($"Writing snapshot to: {outputPath}");
            snapshot.WriteToFile(arguments.OutputPath);
        }
    }
}
