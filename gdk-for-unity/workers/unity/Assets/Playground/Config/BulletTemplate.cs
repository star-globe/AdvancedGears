using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class BulletTemplate : MonoBehaviour
    {
        public static EntityTemplate CreateBulletEntityTemplate(Coordinates coords)
        {
            var clientBullet = ClientBulletComponent.Component.CreateSchemaComponentData();
            var workerBullet = WorkerBulletComponent.Component.CreateSchemaComponentData();
            var launchable = Launchable.Component.CreateSchemaComponentData(new EntityId(0));

            var entityBuilder = EntityBuilder.Begin()
                .AddPosition(coords.X, coords.Y, coords.Z, WorkerUtils.UnityGameLogic)
                .AddMetadata("BulletCore", WorkerUtils.UnityGameLogic)
                .SetPersistence(true)
                .SetReadAcl(WorkerUtils.AllWorkerAttributes)
                .AddComponent(clientBullet, WorkerUtils.UnityClient)
                .AddComponent(workerBullet, WorkerUtils.UnityGameLogic)
                .AddComponent(launchable, WorkerUtils.UnityGameLogic);
                //.AddTransformSynchronizationComponents(WorkerUtils.UnityGameLogic,
                //    location: coords.NarrowToUnityVector());

            return entityBuilder.Build();
        }

    }
}

