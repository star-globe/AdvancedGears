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

        public float Highest => this.transform.position.y;

        [SerializeField]
        FieldMaterialType materialType;
        public FieldMaterialType MaterialType => materialType;

        [SerializeField]
        int seeds;
        public int Seeds => seeds;

        public FieldSnapshot GetFieldSnapshot(float horizontalRate, float virticalRate, float maxRange)
        {
            var pos = this.transform.position;
            return new FieldSnapshot()
            {
                highest = pos.y * virticalRate,
                range = Mathf.Max(range * horizontalRate, maxRange),
                materialType = materialType,
                pos = new Vector3(pos.x * horizontalRate, pos.y * virticalRate, pos.z * horizontalRate),
                seeds = this.seeds,
            };
        }
    }
}
