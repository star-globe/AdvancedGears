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
            template.AddComponent(new BaseUnitMovement.Snapshot(new Vector3f { X = -2.0f }), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Launchable.Snapshot(), WorkerUtils.UnityGameLogic);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
            //var cubeColor = CubeColor.Component.CreateSchemaComponentData();
            //var baseUnit = BaseUnit.Component.CreateSchemaComponentData(side, new Vector3f { X = -2.0f });
            //var launchable = Launchable.Component.CreateSchemaComponentData(new EntityId(0));
            //
            //var entityBuilder = EntityBuilder.Begin()
            //    .AddPosition(coords.X, coords.Y, coords.Z, WorkerUtils.UnityGameLogic)
            //    .AddMetadata("BaseUnit", WorkerUtils.UnityGameLogic)
            //    .SetPersistence(true)
            //    .SetReadAcl(WorkerUtils.AllWorkerAttributes)
            //    //.AddComponent(cubeColor, WorkerUtils.UnityGameLogic)
            //    .AddComponent(baseUnit, WorkerUtils.UnityGameLogic)
            //    .AddComponent(launchable, WorkerUtils.UnityGameLogic)
            //    .AddTransformSynchronizationComponents(WorkerUtils.UnityGameLogic,
            //        location: coords.NarrowToUnityVector());
            //
            //return entityBuilder.Build();
        }
    }
}
