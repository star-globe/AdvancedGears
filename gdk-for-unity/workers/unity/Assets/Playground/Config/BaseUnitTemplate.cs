using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;

namespace Playground
{
    public class BaseUnitTemplate
    {
        public static EntityTemplate CreateBaseUnitEntityTemplate(uint side, Coordinates coords)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot("BaseUnit"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitMovement.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitAction.Snapshot { EnemyPositions = new List<Vector3f>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitStatus.Snapshot(side, UnitState.Alive, ActState.Idle), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitSight.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Launchable.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitHealth.Snapshot(10,10), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BulletComponent.Snapshot(), WorkerUtils.UnityGameLogic);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
