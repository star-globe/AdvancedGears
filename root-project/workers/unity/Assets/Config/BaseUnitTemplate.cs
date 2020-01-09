using System.Collections.Generic;
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
        };

        static readonly Dictionary<UnitType, OrderType> orderDic = new Dictionary<UnitType, OrderType>()
        {
            { UnitType.Soldier, OrderType.Idle },
            { UnitType.Commander, OrderType.Attack },
            { UnitType.Stronghold, OrderType.Idle },
            { UnitType.HeadQuarter, OrderType.Attack },
        };

        public static EntityTemplate CreateBaseUnitEntityTemplate(UnitSide side, Coordinates coords, UnitType type, OrderType? order = null)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot(metaDic[type]), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitMovement.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitAction.Snapshot { EnemyPositions = new List<FixedPointVector3>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitStatus.Snapshot(side, type, UnitState.Alive, order == null ? orderDic[type]: order.Value), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitSight.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitTarget.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Launchable.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new GunComponent.Snapshot { GunsDic = new Dictionary<PosturePoint, GunInfo>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new FuelComponent.Snapshot(), WorkerUtils.UnityGameLogic);

            if (type.BaseType() == UnitBaseType.Moving)
                template.AddComponent(new BaseUnitReviveTimer.Snapshot { IsStart = false, RestTime = 0.0f }, WorkerUtils.UnityGameLogic);
            
            SwitchType(template, type, WorkerUtils.UnityGameLogic);
            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(WorkerUtils.AllWorkerAttributes.ToArray());
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
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
                    template.AddComponent(new CommanderStatus.Snapshot { Order = new OrderPair { Self = OrderType.Idle, Upper = OrderType.Idle },
                                                                         Rank = 0, }, writeAccess);
                    template.AddComponent(new CommanderTeam.Snapshot { FollowerInfo = new FollowerInfo { Followers = new List<EntityId>(), UnderCommanders = new List<EntityId>() },
                                                                       SuperiorInfo = new SuperiorInfo() }, writeAccess);
                    template.AddComponent(new CommanderSight.Snapshot { WarPowers = new List<WarPower>() }, writeAccess);
                    template.AddComponent(new CommanderAction.Snapshot { ActionType = CommandActionType.None }, writeAccess);
                    template.AddComponent(new BaseUnitPosture.Snapshot { Posture = new PostureInfo { Datas = new Dictionary<PosturePoint, PostureData>() } }, writeAccess);
                    template.AddComponent(new DominationDevice.Snapshot { Type = DominationDeviceType.Capturing, Speed = 0.5f, }, writeAccess);
                    break;

                case UnitType.Stronghold:
                    template.AddComponent(new StrongholdStatus.Snapshot { Rank = 1, }, writeAccess);
                    template.AddComponent(new StrongholdSight.Snapshot(), writeAccess);
                    template.AddComponent(new UnitFactory.Snapshot { FollowerOrders = new List<FollowerOrder>(),
                                                                     SuperiorOrders = new List<SuperiorOrder>(),
                                                                     TeamOrders = new List<TeamOrder>() }, writeAccess);
                    template.AddComponent(new UnitArmyObserver.Snapshot(), writeAccess);
                    template.AddComponent(new DominationStamina.Snapshot { SideStaminas = new Dictionary<UnitSide,float>() }, writeAccess);
                    template.AddComponent(new SpawnPoint.Snapshot { Type = SpawnType.Revive }, writeAccess);
                    var commandersQuery = InterestQuery.Query(Constraint.Component<CommanderStatus.Component>())
                                            .FilterResults(Position.ComponentId, BaseUnitStatus.ComponentId);
                    var commanderInterest = InterestTemplate.Create().AddQueries<StrongholdStatus.Component>(commandersQuery);
                    template.AddComponent(commanderInterest.ToSnapshot(), writeAccess);
                    break;

                case UnitType.HeadQuarter:
                    //template.AddComponent(new HeadQuarters.Snapshot { UpperRank = 0,
                    //                                                 FactoryDatas = new FactoryMap { Reserves = new Dictionary<EntityId,ReserveMap>() },
                    //                                                 Orders = new List<OrganizeOrder>() }, writeAccess);
                    template.AddComponent(new CommandersManager.Snapshot { State = CommanderManagerState.None,
                                                                           CommanderDatas = new Dictionary<EntityId, TeamInfo>() }, writeAccess);
                    var strongholdQuery = InterestQuery.Query(Constraint.Component<StrongholdStatus.Component>())
                                          .FilterResults(Position.ComponentId, BaseUnitStatus.ComponentId);
                    var strongholdInterest = InterestTemplate.Create().AddQueries<CommandersManager.Component>(strongholdQuery);
                    template.AddComponent(strongholdInterest.ToSnapshot(), writeAccess);
                    template.AddComponent(new SpawnPoint.Snapshot { Type = SpawnType.Start }, writeAccess);
                    break;
            }
        }

        public static EntityTemplate CreateCommanderUnitEntityTemplate(UnitSide side, Coordinates coords, uint rank, EntityId? superiorId)
        {
            var template = CreateBaseUnitEntityTemplate(side, coords, UnitType.Commander);
            var status = template.GetComponent<CommanderStatus.Snapshot>();
            if (status != null) {
                var s = status.Value;
                s.Rank = rank;

                template.SetComponent(s);
            }

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
            template.AddComponent(new Metadata.Snapshot(isPlayer ? "Player": "AdvancedUnit"), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BulletComponent.Snapshot(), controllAttribute);
            template.AddComponent(new AdvancedUnitController.Snapshot(), controllAttribute);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new GunComponent.Snapshot { GunsDic = new Dictionary<PosturePoint, GunInfo>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new FuelComponent.Snapshot(), WorkerUtils.UnityGameLogic);

            if (isPlayer) {
                template.AddComponent(new AdvancedPlayerInput.Snapshot(), controllAttribute);
                template.AddComponent(new PlayerInfo.Snapshot { ClientWorkerId = workerId }, controllAttribute);
            }
            else { 
                template.AddComponent(new AdvancedUnmannedInput.Snapshot(), controllAttribute);
            }

            template.AddComponent(new BaseUnitStatus.Snapshot { Type = UnitType.Advanced, Side = side, State = UnitState.Alive }, WorkerUtils.UnityGameLogic);

            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, controllAttribute);
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, WorkerUtils.UnityGameLogic);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
