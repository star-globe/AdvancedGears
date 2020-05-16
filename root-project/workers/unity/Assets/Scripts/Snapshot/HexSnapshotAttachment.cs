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

        readonly string writeAccess = WorkerUtils.UnityGameLogic;
        public override void AddComponent(EntityTemplate template)
        {
            template.AddComponent(new HexFacility.Snapshot(hexIndex: hexIndex), writeAccess);

            bool isDominatable = false;
            bool isFactory = false;

            switch (attribute)
            {
                case HexAttribute.Field:
                    isDominatable = true;
                    break;
                case HexAttribute.ForwardBase:
                    isDominatable = true;
                    isFactory = true;
                    break;
                case HexAttribute.CentralBase:
                    isFactory = true;
                    break;
                case HexAttribute.NotBelong:
                    break;
            }

            if (isDominatable)
                template.AddComponent(new DominationStamina.Snapshot().DefaultSnapshot(), writeAccess);

            if (isFactory)
                template.AddComponent(new UnitFactory.Snapshot().DefaultSnapshot(), writeAccess);
        }
    }
}
