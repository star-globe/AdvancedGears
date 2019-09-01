using System;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class FieldTemplate
    {
        public static EntityTemplate CreateFieldEntityTemplate(Coordinates coords, float size, FieldMaterialType fieldMaterialType = FieldMaterialType.None)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("Ground"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new FieldComponent.Snapshot { Size = size, FieldMatType = fieldMaterialType }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
