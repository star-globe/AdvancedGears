using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;
using AdvancedGears.UI;

namespace AdvancedGears
{
    public static class WorkerUtils
    {
        public const string UnityClient = "UnityClient";
        public const string UnityGameLogic = "UnityGameLogic";
        public const string UnityStrategyLogic = "UnityStrategyLogic";
        public const string MobileClient = "MobileClient";
        public static IEnumerable <string> AllPhysicalAttributes
        {
            get
            {
                yield return UnityGameLogic;
                yield return UnityClient;
                yield return MobileClient;
            }
        }

        public static IEnumerable <string> AllWorkerAttributes
        {
            get
            {
                foreach(var p in AllPhysicalAttributes)
                    yield return p;

                yield return UnityStrategyLogic;
            }
        }

        public static void AddClientSystems(World world, bool autoRequestPlayerCreation = true)
        {
            TransformSynchronizationHelper.AddClientSystems(world);
            PlayerLifecycleHelper.AddClientSystems(world, autoRequestPlayerCreation);
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
            world.GetOrCreateSystem<SpawnPointQuerySystem>();
            world.GetOrCreateSystem<UnitUIInfoSystem>();
            world.GetOrCreateSystem<MiniMapUISystem>();
        }

        public static void AddGameLogicSystems(World world)
        {
            TransformSynchronizationHelper.AddServerSystems(world);
            PlayerLifecycleHelper.AddServerSystems(world);
            world.GetOrCreateSystem<ProcessLaunchCommandSystem>();
            world.GetOrCreateSystem<ProcessRechargeSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<ProcessScoresSystem>();
            world.GetOrCreateSystem<CollisionProcessSystem>();
            world.GetOrCreateSystem<RootPostureSyncSystem>();
            world.GetOrCreateSystem<BaseUnitMovementSystem>();
            world.GetOrCreateSystem<BaseUnitSightSystem>();
            world.GetOrCreateSystem<BaseUnitPhysicsSystem>();
            world.GetOrCreateSystem<BaseUnitSearchSystem>();
            world.GetOrCreateSystem<BaseUnitActionSystem>();
            world.GetOrCreateSystem<CommanderUnitSearchSystem>();
            world.GetOrCreateSystem<CommanderActionSystem>();
            world.GetOrCreateSystem<BoidsUpdateSystem>();
            world.GetOrCreateSystem<UnitFactorySystem>();
            world.GetOrCreateSystem<StrategyOrderManagerSystem>();
            world.GetOrCreateSystem<ResourceSupplyManagerSystem>();
            //world.GetOrCreateSystem<HQOrganizeSystem>();
            //world.GetOrCreateSystem<UnitArmyObserveSystem>();
            //world.GetOrCreateSystem<CommandersManagerSystem>();
            world.GetOrCreateSystem<DominationSystem>();
            world.GetOrCreateSystem<StrongholdSearchSystem>();
            world.GetOrCreateSystem<StrongholdActionSystem>();
            world.GetOrCreateSystem<BulletMovementSystem>();
            world.GetOrCreateSystem<BaseUnitReviveTimerSystem>();
            world.GetOrCreateSystem<FieldQueryServerSystem>();
            world.GetOrCreateSystem<HexUpdateSystem>();
        }

        public static void AddStrategyLogicSystems(World world, EntityRepresentationMapping entityRepresentationMapping)
        {
            TransformSynchronizationHelper.AddServerSystems(world);
            PlayerLifecycleHelper.AddServerSystems(world);
            GameObjectCreationHelper.EnableStandardGameObjectCreation(world, entityRepresentationMapping);

            world.GetOrCreateSystem<ProcessLaunchCommandSystem>();
            world.GetOrCreateSystem<MetricSendSystem>();
            world.GetOrCreateSystem<ArmyCloudUpdateSystem>();
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
