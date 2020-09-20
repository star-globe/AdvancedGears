using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace AdvancedGears.Editor
{
    [CustomEditor(typeof(DictionaryPublisher))]
    public class DictionaryPublisherEditor : UnityEditor.Editor
    {
        DictionaryPublisher publisher = null;
        void OnEnable()
        {
            publisher = target as DictionaryPublisher;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (publisher == null)
                return;

            var path = publisher.DictionaryPath;

            if (string.IsNullOrEmpty(path))
                return;

            if (GUILayout.Button("Attach Dictionary") == false)
                return;

            publisher.ClearDictionaries();

            var folders = new List<string>() { path };
            folders.AddRange(AssetDatabase.GetSubFolders(path));

            foreach(var f in folders) {
                var pathes = System.IO.Directory.GetFiles(f);
                foreach (var p in pathes)
                {
                    var dic = AssetDatabase.LoadAssetAtPath(p, typeof(DictionarySettings)) as DictionarySettings;
                    if (dic == null)
                        continue;

                    publisher.AddDictionary(dic);
                }
            }
        }
    }
}
