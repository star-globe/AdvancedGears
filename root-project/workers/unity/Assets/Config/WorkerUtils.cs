using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;

namespace AdvancedGears
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

        public static void AddClientSystems(World world, UnityEngine.GameObject gameObject, bool autoRequestPlayerCreation = true)
        {
            TransformSynchronizationHelper.AddClientSystems(world);
            PlayerLifecycleHelper.AddClientSystems(world, autoRequestPlayerCreation);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world, gameObject);
            world.GetOrCreateSystem<ProcessColorChangeSystem>();
            world.GetOrCreateSystem<AdvancedPlayerInputSync>();
            world.GetOrCreateSystem<MoveAdvancedUnitSystem>();
            world.GetOrCreateSystem<InitCameraSystem>();
            world.GetOrCreateSystem<FollowCameraSystem>();
            world.GetOrCreateSystem<InitUISystem>();
            world.GetOrCreateSystem<UpdateUISystem>();
            world.GetOrCreateSystem<PlayerCommandsSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<BulletMovementSystem>();
            world.GetOrCreateSystem<FieldQueryClientSystem>();
        }

        public static void AddGameLogicSystems(World world, UnityEngine.GameObject gameObject)
        {
            TransformSynchronizationHelper.AddServerSystems(world);
            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world, gameObject);

            world.GetOrCreateSystem<ProcessLaunchCommandSystem>();
            world.GetOrCreateSystem<ProcessRechargeSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<ProcessScoresSystem>();
            world.GetOrCreateSystem<CollisionProcessSystem>();
            world.GetOrCreateSystem<BaseUnitMovementSystem>();
            world.GetOrCreateSystem<BaseUnitSearchSystem>();
            world.GetOrCreateSystem<BaseUnitActionSystem>();
            world.GetOrCreateSystem<CommanderUnitSearchSystem>();
            world.GetOrCreateSystem<CommanderActionSystem>();
            world.GetOrCreateSystem<UnitFactorySystem>();
            //world.GetOrCreateSystem<HQOrganizeSystem>();
            //world.GetOrCreateSystem<UnitArmyObserveSystem>();
            world.GetOrCreateSystem<CommandersManagerSystem>();
            world.GetOrCreateSystem<BulletMovementSystem>();
            world.GetOrCreateSystem<BaseUnitReviveTimerSystem>();
            world.GetOrCreateSystem<FieldQueryServerSystem>();
        }
    }

    public static class Utils
    {
        public static EntityId EmptyEntityId
        {
            get { return new EntityId(0); }
        }
    }
}
