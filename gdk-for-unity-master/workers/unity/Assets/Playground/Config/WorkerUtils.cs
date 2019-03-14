using Improbable.Gdk.GameObjectRepresentation;
using Improbable.Gdk.GameObjectCreation;
using System.Collections.Generic;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;

namespace Playground
{
    public static class WorkerUtils
    {
        public const string UnityClient = "UnityClient";
        public const string UnityGameLogic = "UnityGameLogic";

        public static readonly List<string> AllWorkerAttributes =
            new List<string>
            {
                UnityGameLogic,
                UnityClient
            };

        public static void AddClientSystems(World world)
        {
            AddLifecycleSystems(world);
            TransformSynchronizationHelper.AddClientSystems(world);
            PlayerLifecycleHelper.AddClientSystems(world);
            GameObjectRepresentationHelper.AddSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);
            world.GetOrCreateManager<ProcessColorChangeSystem>();
            world.GetOrCreateManager<LocalPlayerInputSync>();
            world.GetOrCreateManager<MoveLocalPlayerSystem>();
            world.GetOrCreateManager<InitCameraSystem>();
            world.GetOrCreateManager<FollowCameraSystem>();
            world.GetOrCreateManager<InitUISystem>();
            world.GetOrCreateManager<UpdateUISystem>();
            world.GetOrCreateManager<PlayerCommandsSystem>();
            world.GetOrCreateManager<MetricSendSystem>();
            world.GetOrCreateManager<BulletMovementSystem>();
        }

        public static void AddGameLogicSystems(World world)
        {
            AddLifecycleSystems(world);
            TransformSynchronizationHelper.AddServerSystems(world);
            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectRepresentationHelper.AddSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);
            world.GetOrCreateManager<CubeMovementSystem>();
            world.GetOrCreateManager<TriggerColorChangeSystem>();
            world.GetOrCreateManager<ProcessLaunchCommandSystem>();
            world.GetOrCreateManager<ProcessRechargeSystem>();
            world.GetOrCreateManager<MetricSendSystem>();
            world.GetOrCreateManager<ProcessScoresSystem>();
            world.GetOrCreateManager<CollisionProcessSystem>();
            world.GetOrCreateManager<BaseUnitMovementSystem>();
            world.GetOrCreateManager<BulletMovementSystem>();
        }

        private static void AddLifecycleSystems(World world)
        {
            world.GetOrCreateManager<DisconnectSystem>();
        }
    }
}
