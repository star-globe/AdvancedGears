using System;
using System.Linq;
using Improbable.Gdk.Core;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears
{
    public class HexSnapshotAttachment: UnitSnapshotAttachment
    {
        public uint hexIndex;
        public HexAttribute attribute;
        public override void AddComponent(EntityTemplate template)
        {
            template.AddComponent(new HexFacility.Snapshot(hexIndex: hexIndex));

            //switch (attribute)
            //{
            //    case HexAttribute.Field:
            //    case HexAttribute.ForwardBase:
            //    case HexAttribute.
            //}
        }
    }
}
