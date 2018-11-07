using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;
using Improbable.Worker.Core;

namespace Playground
{
    public class BaseUnitTemplate
    {
        public static EntityTemplate CreateBaseUnitEntityTemplate(Coordinates coords)
        {
            //var cubeColor = CubeColor.Component.CreateSchemaComponentData();
            var moveVelocity = BaseUnitMoveVelocity.Component.CreateSchemaComponentData(new Vector3f { X = -2.0f });
            var launchable = Launchable.Component.CreateSchemaComponentData(new EntityId(0));

            var entityBuilder = EntityBuilder.Begin()
                .AddPosition(coords.X, coords.Y, coords.Z, WorkerUtils.UnityGameLogic)
                .AddMetadata("BaseUnit", WorkerUtils.UnityGameLogic)
                .SetPersistence(true)
                .SetReadAcl(WorkerUtils.AllWorkerAttributes)
                //.AddComponent(cubeColor, WorkerUtils.UnityGameLogic)
                .AddComponent(moveVelocity, WorkerUtils.UnityGameLogic)
                .AddComponent(launchable, WorkerUtils.UnityGameLogic)
                .AddTransformSynchronizationComponents(WorkerUtils.UnityGameLogic,
                    location: coords.NarrowToUnityVector());

            return entityBuilder.Build();
        }
    }
}
