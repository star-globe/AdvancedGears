using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using UnityEditor;

namespace AdvancedGears
{
    [CustomEditor(typeof(PostureBoneContainer))]
    public class PostureBoneContainerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var posture = target as PostureBoneContainer;
            if (posture == null)
                return;

            if (GUILayout.Button("SetBonesHash", GUILayout.Width(300)))
            {
            	posture.CheckBonesHash();
            }
        }
    }
}

