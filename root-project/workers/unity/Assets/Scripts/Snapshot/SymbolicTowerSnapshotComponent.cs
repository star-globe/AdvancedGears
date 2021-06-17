using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class SymbolicTowerSnapshotComponent : MonoBehaviour
    {
        [SerializeField]
        UnitSide side;
        public UnitSide Side => side;

        [SerializeField]
        CapsuleCollider capsule;

        public float Height => capsule.height;
        public float Radius => capsule.radius;

        public SymbolicTowerSnapshot GetSymbolicTowerSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new SymbolicTowerSnapshot()
            {
                height = this.Height,
                radius = this.Radius,
                pos = new Vector3(pos.x * horizontalRate, pos.y * virticalRate, pos.z * horizontalRate),
                side = side,
            };
        }
    }

    [Serializable]
    public struct SymbolicTowerSnapshot
    {
        public float height;
        public float radius;
        public Vector3 pos;
        public UnitSide side;
    }
}
