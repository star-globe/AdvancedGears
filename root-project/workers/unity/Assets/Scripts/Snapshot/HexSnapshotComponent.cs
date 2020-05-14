using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class HexSnapshotComponent : MonoBehaviour
    {
        [SerializeField]
        HexAttribute attribute;

        [SerializeField]
        UnitSide side;

        [SerializeField]
        int masterId;

        int index = -1;

        public void SetPosition(Vector3 pos, int index)
        {
            this.index = index;
            this.transform.position = pos;
        }

        public HexSnapshot GetHexSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new HexSnapshot()
            {
            };
        }
    }

    [Serializable]
    public struct HexSnapshot
    {
    }
}
