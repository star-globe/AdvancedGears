using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop.Query;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    class StrategyOrderManagerSystem : EntityQuerySystem
    {
        private Unity.Entities.EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrategyOrderManager.Component>(),
                ComponentType.ReadOnly<StrategyOrderManager.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(10.0f);
        }

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                var list = new IConstraint[]
                {
                    new ComponentConstraint(SpawnPoint.ComponentId),
                };

                return new ImprobableEntityQuery()
                {
                    Constraint = new AndConstraint(list),
                    ResultType = new SnapshotResultType()
                };
            }
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                  ref StrategyOrderManager.Component manager,
                                  ref BaseUnitStatus.Component status,
                                  ref SpatialEntityId spatialEntityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.HeadQuarter)
                    return;

                var inter = manager.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                manager.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var range = RangeDictionary.Get(FixedRangeType.HeadQuarterRange);

                var entityId = new EntityId();
                var enemy = getNearestEnemy(status.Side, trans.position);//, range, allowDead:true, UnitType.HeadQuarter);
                if (enemy == null)
                    return;

                entityId = enemy.id;

                var target = manager.TargetHq;
                if (target.HeadQuarterId != entityId) {
                    manager.TargetHq = new TargetHeadQuartersInfo()
                    {
                        HeadQuarterId = enemy.id,
                        Side = enemy.side,
                        Position = enemy.pos.ToCoordinates(),
                    };
                }

                var st_range = RangeDictionary.Get(FixedRangeType.StrongholdRange);
                var allies = getAllyUnits(status.Side, trans.position);//, st_range, allowDead: false, UnitType.Stronghold);
                foreach(var unit in allies) {
                    var diff = (enemy.pos - unit.pos).normalized;
                    SendCommand(unit.id, enemy.side, diff * st_range);
                }
            });
        }

        void SendCommand(EntityId id, UnitSide side, Vector3 vec)
        {
            this.CommandSystem.SendCommand(new StrongholdSight.SetStrategyVector.Request(
                id,
                new StrategyVector( side, FixedPointVector3.FromUnityVector(vec)))
            );
        }

        UnitInfo getNearestEnemy(UnitSide side, Vector3 pos)
        {
            return null;
        }

        UnitInfo[] getAllyUnits(UnitSide side, Vector3 pos)
        {
            return null;
        }

        protected override void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            if (shots.Count > 0)
            {
                //SetSpawnPoints(shots);
            }
            else
            {
                //SetSpawnPointsClear();
            }

            Debug.LogFormat("EntitySnapshotCount:{0}", shots.Count);
        }
    }
}
