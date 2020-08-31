using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    [CustomEditor (typeof(HexUtilsTest))]
    public class HexUtilsTestEditor : UnityEditor.Editor
    {
        HexUtilsTest utils = null;
        void OnEnable()
        {
            utils = target as HexUtilsTest;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("CalcNeighborIndexes Hexes"))
                utils.CalcNeighborIndexes();

            //if (GUILayout.Button("Convert Hexes"))
            //    strategy.ConvertHexes();
        }
    }
}
