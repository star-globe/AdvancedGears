using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class FieldSnapshotComponent : MonoBehaviour
    {
        [SerializeField]
        float range;
        public float Range => range;

        [SerializeField]
        FieldMaterialType materialType;
        public FieldMaterialType MaterialType => materialType;

        public FieldSnapshot GetFieldSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new FieldSnapshot()
            {
                highest = pos.y * virticalRate,
                range = range * horizontalRate,
                materialType = materialType,
                pos = new Vector3(pos.x * horizontalRate, pos.y * virticalRate, pos.z * horizontalRate),
            };
        }
    }
}
