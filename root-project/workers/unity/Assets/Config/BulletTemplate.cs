using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public class BulletTemplate
    {
        public static EntityTemplate CreateFlareEntityTemplate(Coordinates coords, FlareColorType color, UnitSide side, float startTime)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("FlareObject"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new StrategySmoke.Snapshot { Color = color, LaunchTime = startTime, Side = side }, WorkerUtils.UnityGameLogic);
    
            return template;
        }
    
    }
}

