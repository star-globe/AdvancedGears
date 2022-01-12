using System;
using System.Linq;
using Improbable.Gdk.Core;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class UnitSnapshotLocator : MonoBehaviour
    {
        public UnitSnapshotComponent baseUnit;

        public float radius;
        public int number;

        public void LocateUnits()
        {
            var units = this.GetComponentsInChildren<UnitSnapshotComponent>();
            UnitSnapshotComponent target;
            for (int i = 0; i < number; i++) {
                if (i < units.Length)
                    target = units[i];
                else
                    target = Instantiate(baseUnit, this.transform);

                float rad = Mathf.PI * 2 * i / number;
                target.gameObject.transform.localPosition = new Vector3(radius * Mathf.Cos(rad), 0, radius * Mathf.Sin(rad));
                target.gameObject.SetActive(true);
             }

            for (int i = number; i < units.Length; i++)
                DestroyImmediate(units[i].gameObject);
        }

        public void RenewUnits()
        {
            var units = this.GetComponentsInChildren<UnitSnapshotComponent>();
            for(var i = 0; i < units.Length; i++)
                DestroyImmediate(units[i].gameObject);

            LocateUnits();
        }
    }


#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(UnitSnapshotLocator))]
    public class UnitSnapshotLocatorEditor : UnityEditor.Editor
    {
        UnitSnapshotLocator component = null;

        void OnEnable()
        {
            component = target as UnitSnapshotLocator;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Locate Units"))
                component.LocateUnits();

            if (GUILayout.Button("Renew Units"))
                component.RenewUnits();
        }
    }
#endif
}
