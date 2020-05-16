using System;
using System.Linq;
using Improbable.Gdk.Core;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class UnitSnapshotComponent : MonoBehaviour
    {
        public UnitType type;
        public UnitSide side;

        [SerializeField]
        UnitSnapshotAttachment[] attachments = null;

        public UnitSnapshot GetUnitSnapshot(float horizontalRate, float virticalRate)
        {
            var pos = this.transform.position;
            return new UnitSnapshot()
            {
                type = type,
                side = side,
                pos = new Vector3(pos.x * horizontalRate, pos.y * virticalRate, pos.z * horizontalRate),
                attachments = attachments.ToArray(),
            };
        }

        const float buffer = 0.2f;
        public void SetHeight(float rate)
        {
            var pos = this.transform.position;
            var ray = new Ray(new Vector3(pos.x, 3000.0f, pos.z), Vector3.down);
            RaycastHit hit;
            Physics.Raycast(ray, out hit);

            this.transform.position = new Vector3(pos.x, hit.point.y + buffer / rate, pos.z);
        }
    }

    [Serializable]
    public struct UnitSnapshot
    {
        public UnitType type;
        public UnitSide side;
        public Vector3 pos;
        public UnitSnapshotAttachment[] attachments;
    }
}
