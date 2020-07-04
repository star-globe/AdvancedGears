using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.QueryBasedInterest;
using Improbable.Worker;

namespace AdvancedGears
{
    public class BaseUnitTemplate
    {
        static readonly Dictionary<UnitType, string> metaDic = new Dictionary<UnitType, string>()
        {
            { UnitType.Soldier, "BaseUnit"},
            { UnitType.Commander, "CommanderUnit"},
            { UnitType.Stronghold, "StrongholdUnit"},
            { UnitType.HeadQuarter, "HeadQuarterUnit"},
            { UnitType.ArmyCloud, "ArmyCloudUnit"},
        };

        static readonly Dictionary<UnitType, OrderType> orderDic = new Dictionary<UnitType, OrderType>()
        {
            { UnitType.Soldier, OrderType.Idle },
            { UnitType.Commander, OrderType.Attack },
            { UnitType.Stronghold, OrderType.Idle },
            { UnitType.HeadQuarter, OrderType.Attack },
            { UnitType.ArmyCloud, OrderType.Idle},
        };

        public static EntityTemplate CreateBaseUnitEntityTemplate(UnitSide side, Coordinates coords, UnitType type, OrderType? order = null, uint? rank = null)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot(metaDic[type]), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitMovement.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitSight.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitAction.Snapshot { EnemyPositions = new List<FixedPointVector3>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitStatus.Snapshot(side, type, UnitState.Alive, order == null ? orderDic[type] : order.Value, GetRank(rank, type)), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitTarget.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Launchable.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new GunComponent.Snapshot { GunsDic = new Dictionary<PosturePoint, GunInfo>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new FuelComponent.Snapshot(), WorkerUtils.UnityGameLogic);

            if (type.BaseType() == UnitBaseType.Moving)
                template.AddComponent(new BaseUnitReviveTimer.Snapshot { IsStart = false, RestTime = 0.0f }, WorkerUtils.UnityGameLogic);

            SwitchType(template, type, WorkerUtils.UnityGameLogic);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(GetReadAttributes(type));
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        private static uint GetRank(uint? rank, UnitType type)
        {
            if (rank != null)
                return rank.Value;

            switch (type)
            {
                case UnitType.Stronghold: return 1;
                case UnitType.HeadQuarter: return 2;

                default: return 0;
            }
        }

        private static string[] GetReadAttributes(UnitType type)
        {
            switch (type)
            {
                case UnitType.ArmyCloud:
                    return new string[] { WorkerUtils.UnityStrategyLogic };

                case UnitType.Commander:
                case UnitType.Stronghold:
                case UnitType.HeadQuarter:
                    return WorkerUtils.AllWorkerAttributes.ToArray();

                default:
                    return WorkerUtils.AllPhysicalAttributes.ToArray();
            }
        }

        private static void SwitchType(EntityTemplate template, UnitType type, string writeAccess)
        {
            switch (type) {
                case UnitType.Soldier:
                    template.AddComponent(new BulletComponent.Snapshot(), writeAccess);
                    template.AddComponent(new BaseUnitPosture.Snapshot { Posture = new PostureInfo { Datas = new Dictionary<PosturePoint, PostureData>() } }, writeAccess);
                    break;

                case UnitType.Commander:
                    template.AddComponent(new BulletComponent.Snapshot(), writeAccess);
                    template.AddComponent(new CommanderStatus.Snapshot { Order = new OrderPair { Self = OrderType.Idle, Upper = OrderType.Idle },}, writeAccess);
                    template.AddComponent(new CommanderTeam.Snapshot { FollowerInfo = new FollowerInfo { Followers = new List<EntityId>(), UnderCommanders = new List<EntityId>() },
                        SuperiorInfo = new SuperiorInfo() }, writeAccess);
                    template.AddComponent(new CommanderSight.Snapshot { WarPowers = new List<WarPower>() }, writeAccess);
                    template.AddComponent(new CommanderAction.Snapshot { ActionType = CommandActionType.None }, writeAccess);
                    template.AddComponent(new BaseUnitPosture.Snapshot { Posture = new PostureInfo { Datas = new Dictionary<PosturePoint, PostureData>() } }, writeAccess);
                    template.AddComponent(new DominationDevice.Snapshot { Type = DominationDeviceType.Capturing }, writeAccess);
                    template.AddComponent(new BoidComponent.Snapshot(), writeAccess);
                    break;

                case UnitType.Supply:
                    template.AddComponent(new BulletComponent.Snapshot(), writeAccess);
                    template.AddComponent(new BaseUnitPosture.Snapshot { Posture = new PostureInfo { Datas = new Dictionary<PosturePoint, PostureData>() } }, writeAccess);
                    template.AddComponent(new ResourceComponent.Snapshot(), writeAccess);
                    template.AddComponent(new ResourceTransporter.Snapshot(), writeAccess);
                    break;

                case UnitType.Stronghold:
                    AddStrongholdTypeComponents(template, writeAccess);
                    template.AddComponent(new RecoveryComponent.Snapshot { State = RecoveryState.Supplying }, writeAccess);
                    var commandersQuery = InterestQuery.Query(Constraint.Component<CommanderStatus.Component>())
                                            .FilterResults(Position.ComponentId, BaseUnitStatus.ComponentId);
                    var commanderInterest = InterestTemplate.Create().AddQueries<StrongholdStatus.Component>(commandersQuery);
                    template.AddComponent(commanderInterest.ToSnapshot(), writeAccess);
                    break;

                case UnitType.HeadQuarter:
                    AddStrongholdTypeComponents(template, writeAccess);
                    template.AddComponent(new StrategyOrderManager.Snapshot { }, writeAccess);
                    var strongholdQuery = InterestQuery.Query(Constraint.Component<StrongholdStatus.Component>())
                                          .FilterResults(Position.ComponentId, BaseUnitStatus.ComponentId);
                    var strongholdInterest = InterestTemplate.Create().AddQueries<StrategyOrderManager.Component>(strongholdQuery);
                    template.AddComponent(strongholdInterest.ToSnapshot(), writeAccess);
                    break;

                case UnitType.Turret:
                    template.AddComponent(new TurretComponent.Snapshot(), writeAccess);
                    break;

                case UnitType.ArmyCloud:
                    template.AddComponent(new ArmyCloud.Snapshot { }, writeAccess);
                    break;
            }
        }

        private static void AddStrongholdTypeComponents(EntityTemplate template, string writeAccess)
        {
            template.AddComponent(new StrongholdSight.Snapshot { TargetStrongholds = new Dictionary<EntityId, TargetStrongholdInfo>(),
                                                                 FrontLineCorners = new List<Coordinates>(),
                                                                 TargetHexes = new Dictionary<EntityId, TargetHexInfo>() }, writeAccess);
            template.AddComponent(new StrategyHexAccessPortal.Snapshot { FrontHexes = new Dictionary<UnitSide,FrontHexInfo>() }, writeAccess);
            template.AddComponent(new ResourceComponent.Snapshot(), writeAccess);
            template.AddComponent(new ResourceSupplyer.Snapshot(), writeAccess);
            template.AddComponent(new TurretHub.Snapshot { TurretsDatas = new Dictionary<EntityId,TurretInfo>() }, writeAccess);
        }

        public static EntityTemplate CreateCommanderUnitEntityTemplate(UnitSide side, Coordinates coords, uint rank, EntityId? superiorId)
        {
            var template = CreateBaseUnitEntityTemplate(side, coords, UnitType.Commander, rank:rank);
            var team = template.GetComponent<CommanderTeam.Snapshot>();
            if (team != null) {
                var t = team.Value;

                if (superiorId != null)
                    t.SuperiorInfo.EntityId = superiorId.Value;

                template.SetComponent(t);
            }

            return template;
        }

        public static EntityTemplate CreateAdvancedUnitEntityTemplate(string workerId, Coordinates coords, UnitSide side)
        {
            bool isPlayer = workerId != null;
            string controllAttribute;
            if (isPlayer)
                controllAttribute = EntityTemplate.GetWorkerAccessAttribute(workerId);
            else
                controllAttribute = WorkerUtils.UnityGameLogic;

            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot { Coords = coords }, controllAttribute);
            template.AddComponent(new Metadata.Snapshot(isPlayer ? "Player" : "AdvancedUnit"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BulletComponent.Snapshot(), controllAttribute);
            template.AddComponent(new AdvancedUnitController.Snapshot(), controllAttribute);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new GunComponent.Snapshot { GunsDic = new Dictionary<PosturePoint, GunInfo>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new FuelComponent.Snapshot(), WorkerUtils.UnityGameLogic);

            InterestTemplate interest = InterestTemplate.Create();

            if (isPlayer) {
                template.AddComponent(new AdvancedPlayerInput.Snapshot(), controllAttribute);
                template.AddComponent(new PlayerInfo.Snapshot { ClientWorkerId = workerId }, controllAttribute);
                PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);

                // ミニマップ用QBI
                template.AddComponent(new MinimapComponent.Snapshot(), controllAttribute);
                AddMinimapQuery<MinimapComponent.Component>(interest);
            }
            else {
                template.AddComponent(new AdvancedUnmannedInput.Snapshot(), controllAttribute);
            }

            template.AddComponent(new BaseUnitStatus.Snapshot { Type = UnitType.Advanced, Side = side, State = UnitState.Alive }, WorkerUtils.UnityGameLogic);

            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, controllAttribute);

            // 共通QBI
            AddBasicQuery<Position.Component>(interest);
            template.AddComponent(interest.ToSnapshot(), controllAttribute);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, WorkerUtils.UnityGameLogic);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }

        public static EntityTemplate CreateTurretUnitTemplate(UnitSide side, Coordinates coords, int masterId)
        {
            var template = CreateBaseUnitEntityTemplate(side, coords, UnitType.Turret);
            var turret = template.GetComponent<TurretComponent.Snapshot>();
            if (turret != null) {
                var t = turret.Value;
                t.MasterId = masterId;

                template.SetComponent(t);
            }

            return template;
        }

        static void AddBasicQuery<T>(InterestTemplate interest) where T : ISpatialComponentData
        {
            var basicQuery = InterestQuery.Query(Constraint.RelativeSphere(FixedParams.PlayerInterestLimit));

            interest.AddQueries<T>(basicQuery);
        }

        static void AddMinimapQuery<T>(InterestTemplate interest) where T : ISpatialComponentData
        {
            var minimapQuery = InterestQuery.Query(
               Constraint.All(
                   Constraint.Any(Constraint.Component(CommanderStatus.ComponentId),
                                  Constraint.Component(StrongholdStatus.ComponentId),
                                  Constraint.Component(HeadQuarters.ComponentId)),
                   Constraint.RelativeSphere(FixedParams.WorldInterestLimit)))
               .FilterResults(Position.ComponentId,
                              BaseUnitStatus.ComponentId,
                              TransformInternal.ComponentId)
               .WithMaxFrequencyHz(FixedParams.WorldInterestFrequency);

            interest.AddQueries<T>(minimapQuery);
        }
    }

    static class TemplateExtensions
    {
        public static UnitFactory.Snapshot DefaultSnapshot(this UnitFactory.Snapshot snapshot)
        {
            snapshot.Containers = new List<UnitContainer>();
            snapshot.FollowerOrders = new List<FollowerOrder>();
            snapshot.SuperiorOrders = new List<SuperiorOrder>();
            snapshot.TeamOrders = new List<TeamOrder>();
            snapshot.TurretOrders = new List<TurretOrder>();
            return snapshot;
        }

        public static DominationStamina.Snapshot DefaultSnapshot(this DominationStamina.Snapshot snapshot)
        {
            snapshot.SideStaminas = new Dictionary<UnitSide, float>();
            return snapshot;
        }
    }
}
