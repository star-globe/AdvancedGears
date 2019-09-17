using System.IO;
using UnityEditor;
using UnityEngine;

#if false
namespace Playground.Editor.SnapshotGenerator
{
    internal class SnapshotEditorWindow : EditorWindow
    {
        private SnapshotGenerator.Arguments arguments;

        [MenuItem("SpatialOS/Generate snapshot", false, 200)]
        public static void GenerateMenuItem()
        {
            GetWindow<SnapshotEditorWindow>().Show();
        }

        public void Awake()
        {
            minSize = new Vector2(200, 120);
            titleContent = new GUIContent("Generate snapshot");

            SetDefaults();
        }

        private void SetDefaults()
        {
            arguments = new SnapshotGenerator.Arguments
            {
                OutputPath = SnapshotGenerator.DefaultSnapshotPath
            };
        }

        public void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (GUILayout.Button("Defaults"))
                {
                    SetDefaults();
                    Repaint();
                }

                arguments.OutputPath = EditorGUILayout.TextField("Snapshot path", arguments.OutputPath);

                var shouldDisable = string.IsNullOrEmpty(arguments.OutputPath);
                using (new EditorGUI.DisabledScope(shouldDisable))
                {
                    if (GUILayout.Button("Generate snapshot"))
                    {
                        SnapshotGenerator.Generate(arguments);
                    }
                }
            }
        }
    }
}

#endif

