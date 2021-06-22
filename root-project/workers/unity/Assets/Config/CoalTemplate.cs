using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class CoalTemplate
    {
        public static EntityTemplate CreateCoalSolidsEntityTemplate(Coordinates coords, int amount)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("CoalSolids"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new CoalSolids.Snapshot { Amount = amount }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
