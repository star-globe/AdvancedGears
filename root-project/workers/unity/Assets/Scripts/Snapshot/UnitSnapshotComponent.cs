using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class UnitSnapshotComponent : MonoBehaviour
    {
        [SerializeField]
        UnitType type;

        [SerializeField]
        UnitSide side;

        public UnitSnapshot GetUnitSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new UnitSnapshot()
            {
                type = type,
                side = side,
                pos = new Vector3(pos.x * horizontalRate, pos.y * virticalRate, pos.z * horizontalRate),
            };
        }
    }
}
