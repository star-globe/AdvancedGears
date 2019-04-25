using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker;

namespace Playground
{
    public class BaseUnitTemplate
    {
        static readonly Dictionary<UnitType, string> metaDic = new Dictionary<UnitType, string>()
        {
            { UnitType.Soldier, "BaseUnit"},
            { UnitType.Commander, "CommanderUnit"},
            { UnitType.Stronghold, "StrongholdUnit"},
        };

        static readonly Dictionary<UnitType, OrderType> orderDic = new Dictionary<UnitType, OrderType>()
        {
            { UnitType.Soldier, OrderType.Idle },
            { UnitType.Commander, OrderType.Attack },
            { UnitType.Stronghold, OrderType.Idle },
        };

        public static EntityTemplate CreateBaseUnitEntityTemplate(UnitSide side, Coordinates coords, UnitType type)
        {
            var template = new EntityTemplate();
            template.AddComponent(new Position.Snapshot(coords), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Metadata.Snapshot(metaDic[type]), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Persistence.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitMovement.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitAction.Snapshot { EnemyPositions = new List<Vector3f>() }, WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitStatus.Snapshot(side, type, UnitState.Alive, orderDic[type]), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitSight.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitTarget.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new Launchable.Snapshot(), WorkerUtils.UnityGameLogic);
            template.AddComponent(new BaseUnitHealth.Snapshot(), WorkerUtils.UnityGameLogic);
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
                    break;

                case UnitType.Commander:
                    template.AddComponent(new BulletComponent.Snapshot(), writeAccess);
                    template.AddComponent(new CommanderStatus.Snapshot { Followers = new List<EntityId>(), SelfOrder = OrderType.Idle }, writeAccess);
                    template.AddComponent(new CommanderSight.Snapshot { WarPowers = new List<WarPower>() }, writeAccess);
                    break;

                case UnitType.Stronghold:
                    template.AddComponent(new UnitFactoryComponent.Snapshot { Orders = new List<ProductOrder>() }, writeAccess);
                    break;
            }
        }
    }
}
