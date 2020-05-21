using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    [CustomEditor (typeof(StrategySnapshotScene))]
    public class StrategySnapshotSceneEditor : SnapshotSceneEditor
    {
        protected override void AttachScene()
        {
            scene = target as StrategySnapshotScene;
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Align Hexes"))
                scene.AlignHexes();

            if (GUILayout.Button("Convert Hexes"))
                scene.ConvertHexes();
        }
    }
}
