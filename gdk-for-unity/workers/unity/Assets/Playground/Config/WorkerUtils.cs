using Improbable.Gdk;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;

namespace Playground
{
    public static class WorkerUtils
    {
        public const string UnityClient = "UnityClient";
        public const string UnityGameLogic = "UnityGameLogic";
        public const string MobileClient = "MobileClient";
        public static readonly List<string> AllWorkerAttributes =
            new List<string>
            {
                UnityGameLogic,
                UnityClient,
                MobileClient
            };

        public static void AddClientSystems(World world)
        {
            AddLifecycleSystems(world);
            TransformSynchronizationHelper.AddClientSystems(world);
            PlayerLifecycleHelper.AddClientSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);
            world.GetOrCreateSystem<ProcessColorChangeSystem>();
            world.GetOrCreateSystem<LocalPlayerInputSync>();
            world.GetOrCreateSystem<MoveLocalPlayerSystem>();
            world.GetOrCreateSystem<InitCameraSystem>();
            world.GetOrCreateSystem<FollowCameraSystem>();
            world.GetOrCreateSystem<InitUISystem>();
            world.GetOrCreateSystem<UpdateUISystem>();
            world.GetOrCreateSystem<PlayerCommandsSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<BulletMovementSystem>();
        }

        public static void AddGameLogicSystems(World world)
        {
            AddLifecycleSystems(world);
            TransformSynchronizationHelper.AddServerSystems(world);
            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world);

            world.GetOrCreateSystem<TriggerColorChangeSystem>();
            world.GetOrCreateSystem<ProcessLaunchCommandSystem>();
            world.GetOrCreateSystem<ProcessRechargeSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<ProcessScoresSystem>();
            world.GetOrCreateSystem<CollisionProcessSystem>();
            world.GetOrCreateSystem<CubeMovementSystem>();
            world.GetOrCreateSystem<BaseUnitMovementSystem>();
            world.GetOrCreateSystem<BaseUnitSearchSystem>();
            world.GetOrCreateSystem<BaseUnitActionSystem>();
            world.GetOrCreateSystem<CommanderUnitSearchSystem>();
        }

        private static void AddLifecycleSystems(World world)
        {
            world.GetOrCreateSystem<DisconnectSystem>();
        }
    }
}
