using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using UnityEditor;

namespace Playground
{
    [CustomEditor(typeof(PostureTransform))]
    public class PostureTransformEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var posture = target as PostureTransform;
            if (posture == null)
                return;

            //if (GUILayout.Button("Connector検索", GUILayout.Width(300)))
            //{
            //	posture.CheckConnectors();
            //}
        }
    }
}

