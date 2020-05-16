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
    }
}
