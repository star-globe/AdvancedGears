using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using UnityEditor;

namespace AdvancedGears
{
    [CustomEditor(typeof(PostureBonesContainer))]
    public class PostureBonesContainerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var posture = target as PostureBonesContainer;
            if (posture == null)
                return;

            if (GUILayout.Button("SetBonesHash", GUILayout.Width(300)))
            {
            	posture.CheckBonesHash();
            }
        }
    }
}

