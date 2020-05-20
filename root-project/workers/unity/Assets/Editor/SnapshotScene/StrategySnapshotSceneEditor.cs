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
        protected override void GenerateSnapshot()
        {
            if (scene == null)
                return;

            var arguments = new SnapshotGenerator.Arguments
            {
                OutputPath = SnapshotGenerator.GetSnapshotPath(outputPath)
            };

            var snapshot = SnapshotGenerator.GenerateGroundSnapshot(scene.WorldSize, scene.GetHeight);//, scene.Units, scene.Fields);
        }
    }
}
