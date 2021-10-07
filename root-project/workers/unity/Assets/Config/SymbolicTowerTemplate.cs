using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class SymbolicTowerTemplate
    {
        public static EntityTemplate CreateSymbolicTowerEntityTemplate(Coordinates coords, float height, float radius, UnitSide side)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("Tower"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new SymbolicTower.Snapshot { Side = side, Height = height, Radius = radius, State = TowerState.Idle }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
