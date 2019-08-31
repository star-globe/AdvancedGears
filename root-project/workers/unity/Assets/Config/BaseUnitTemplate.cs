using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.PlayerLifecycle;
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
            { UnitType.Stronghold, OrderType.Organize },
            { UnitType.HeadQuarter, OrderType.Organize },
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
                    template.AddComponent(new CommanderStatus.Snapshot { FollowerInfo = new FollowerInfo { Followers = new List<EntityId>(), UnderCommanders = new List<EntityId>() },
                                                                         SuperiorInfo = new SuperiorInfo(),
                                                                         Order = new OrderPair { Self = OrderType.Idle, Upper = OrderType.Idle },
                                                                         Rank = 0, }, writeAccess);
                    template.AddComponent(new CommanderSight.Snapshot { WarPowers = new List<WarPower>() }, writeAccess);
                    template.AddComponent(new CommanderAction.Snapshot { ActionType = CommandActionType.None }, writeAccess);
                    template.AddComponent(new BaseUnitPosture.Snapshot { Posture = new PostureInfo { Datas = new Dictionary<PosturePoint, PostureData>() } }, writeAccess);
                    template.AddComponent(new DominationDevice.Snapshot { Type = DominationDeviceType.Capturing, Speed = 0.0f, }, writeAccess);
                    break;

                case UnitType.Stronghold:
                    template.AddComponent(new UnitFactory.Snapshot { FollowerOrders = new List<FollowerOrder>(), SuperiorOrders = new List<SuperiorOrder>() }, writeAccess);
                    template.AddComponent(new UnitArmyObserver.Snapshot(), writeAccess);
                    template.AddComponent(new DominationStamina.Snapshot { SideStaminas = new Dictionary<UnitSide,float>() }, writeAccess);
                    break;

                case UnitType.HeadQuarter:
                    template.AddComponent(new HeadQuarters.Snapshot { UpperRank = 0,
                                                                     FactoryDatas = new FactoryMap { Reserves = new Dictionary<EntityId,ReserveMap>() },
                                                                     Orders = new List<OrganizeOrder>() }, writeAccess);
                    break;
            }
        }

        public static EntityTemplate CreateCommanderUnitEntityTemplate(UnitSide side, Coordinates coords, uint rank, EntityId? superiorId)
        {
            var template = CreateBaseUnitEntityTemplate(side, coords, UnitType.Commander);
            var snap = template.GetComponent<CommanderStatus.Snapshot>();
            if (snap != null) {
                var s = snap.Value;
                s.Rank = rank;

                if (superiorId != null)
                    s.SuperiorInfo.EntityId = superiorId.Value;

                template.SetComponent(s);
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
            template.AddComponent(new AdvancedUnitController.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new GunComponent.Snapshot { GunsDic = new Dictionary<PosturePoint, GunInfo>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new FuelComponent.Snapshot(), WorkerUtils.UnityGameLogic);

            if (isPlayer)
                template.AddComponent(new AdvancedPlayerInput.Snapshot(), controllAttribute);
            else
                template.AddComponent(new AdvancedUnmannedInput.Snapshot(), controllAttribute);

            template.AddComponent(new BaseUnitStatus.Snapshot { Type = UnitType.Advanced, Side = side, State = UnitState.Alive }, WorkerUtils.UnityGameLogic);

            TransformSynchronizationHelper.AddTransformSynchronizationComponents(template, controllAttribute);
            PlayerLifecycleHelper.AddPlayerLifecycleComponents(template, workerId, WorkerUtils.UnityGameLogic);

            template.SetReadAccess(UnityClientConnector.WorkerType, MobileClientWorkerConnector.WorkerType, WorkerUtils.UnityGameLogic);
            template.SetComponentWriteAccess(EntityAcl.ComponentId, WorkerUtils.UnityGameLogic);

            return template;
        }
    }
}
