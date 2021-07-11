using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public static class HexTemplate
    {
        public static EntityTemplate CreateHexEntityTemplate(Coordinates coords, uint index, HexAttribute attribute, int hexId, UnitSide side)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("HexBase"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new HexBase.Snapshot { Index = index, Attribute = attribute, HexId = hexId, Side = side }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new HexPower.Snapshot() { IsActive = false, SidePowers = new Dictionary<UnitSide, float>() }, WorkerUtils.UnityGameLogic);

            if (hexId % 3 == 0)
                template.AddComponent(new StrategyHexAccessPortal.Snapshot { FrontHexes = new Dictionary<UnitSide, FrontHexInfo>(), HexIndexes = new Dictionary<uint, HexIndex>() }, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
