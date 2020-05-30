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
        StrategySnapshotScene strategy = null;
        protected override void AttachScene()
        {
            strategy = target as StrategySnapshotScene;
            scene = strategy;
        }

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Align Hexes"))
                strategy.AlignHexes();

            if (GUILayout.Button("Convert Hexes"))
                strategy.ConvertHexes();
        }
    }
}
