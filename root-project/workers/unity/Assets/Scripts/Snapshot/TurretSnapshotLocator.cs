using System;
using System.Linq;
using Improbable.Gdk.Core;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class TurretSnapshotLocator : MonoBehaviour
    {
        public UnitSnapshotComponent baseTurret;

        public float radius;
        public int number;

        public void LocateTurrets()
        {
            var units = this.GetComponentsInChildren<UnitSnapshotComponent>();
            UnitSnapshotComponent target;
            for (int i = 0; i < number; i++) {
                if (i < units.Length)
                    target = units[i];
                else
                    target = Instantiate(baseTurret, this.transform);

                float rad = Mathf.PI * 2 * i / number;
                target.gameObject.transform.localPosition = new Vector3(radius * Mathf.Cos(rad), 0, radius * Mathf.Sin(rad));
                target.gameObject.SetActive(true);
             }

            for (int i = number; i < units.Length; i++)
                DestroyImmediate(units[i].gameObject);
        }

        public void RenewTurrets()
        {
            var units = this.GetComponentsInChildren<UnitSnapshotComponent>();
            for(var i = 0; i < units.Length; i++)
                DestroyImmediate(units[i].gameObject);

            LocateTurrets();
        }
    }


#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TurretSnapshotLocator))]
    public class TurretSnapshotLocatorEditor : UnityEditor.Editor
    {
        TurretSnapshotLocator component = null;

        void OnEnable()
        {
            component = target as TurretSnapshotLocator;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Locate Turrets"))
                component.LocateTurrets();

            if (GUILayout.Button("Renew Turrets"))
                component.RenewTurrets();
        }
    }
#endif
}
