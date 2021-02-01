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

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                var list = new IConstraint[]
                {
                    new ComponentConstraint(StrongholdSight.ComponentId),
                };

                return new ImprobableEntityQuery()
                {
                    Constraint = new AndConstraint(list),
                    ResultType = new SnapshotResultType()
                };
            }
        }

        protected override float IntervalTime { get { return 1.0f; } }

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrategyOrderManager.Component>(),
                ComponentType.ReadOnly<StrategyOrderManager.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

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

                var enemy = getNearestEnemy(status.Side, trans.position);
                if (enemy == null)
                    return;

                var entityId = new EntityId();
                entityId = enemy.id;

                var target = manager.TargetHq;
                if (target.HeadQuarterId != entityId) {
                    manager.TargetHq = new TargetHeadQuartersInfo()
                    {
                        HeadQuarterId = enemy.id,
                        Side = enemy.side,
                        Position = enemy.pos.ToWorldCoordinates(this.Origin),
                    };
                }

                var st_range = RangeDictionary.Get(FixedRangeType.StrongholdRange);
                var allies = getAllyUnits(status.Side, trans.position);
                foreach(var unit in allies) {
                    if (unit.type == UnitType.Stronghold)
                        continue;

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
            UnitInfo info = null;
            var length = float.MaxValue;
            foreach (var kvp in sightDictionary)
            {
                if (kvp.Key == side || kvp.Key == UnitSide.None)
                    continue;

                foreach (var unit in kvp.Value)
                {
                    if (unit.type != UnitType.HeadQuarter)
                        continue;

                    var mag = (unit.pos - pos).sqrMagnitude;
                    if (mag < length) {
                        length = mag;
                        info = unit;
                    }
                }
            }

            return info;
        }

        List<UnitInfo> getAllyUnits(UnitSide side, Vector3 pos)
        {
            if (sightDictionary.ContainsKey(side) == false)
                return null;

            return sightDictionary[side];
        }

        protected override void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            if (shots.Count > 0)
            {
                SetStrongholdSights(shots);
            }
            else
            {
                SetStrongholdSightsClear();
            }

            Debug.LogFormat("EntitySnapshotCount:{0}", shots.Count);
        }

        readonly Dictionary<UnitSide,List<UnitInfo>> sightDictionary = new Dictionary<UnitSide,List<UnitInfo>>();
        void SetStrongholdSights(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            SetStrongholdSightsClear();

            foreach (var kvp in shots)
            {
                foreach (var snap in kvp.Value)
                {
                    Position.Snapshot position;
                    if (snap.TryGetComponentSnapshot(out position) == false)
                        continue;

                    BaseUnitStatus.Snapshot status;
                    if (snap.TryGetComponentSnapshot(out status) == false)
                        continue;

                    var info = new UnitInfo();
                    info.id = kvp.Key;
                    info.pos = position.Coords.ToWorkerPosition(this.Origin);
                    info.type = status.Type;
                    info.side = status.Side;
                    info.order = status.Order;
                    info.state = status.State;
                    info.rank = status.Rank;

                    if (sightDictionary.ContainsKey(status.Side) == false)
                        sightDictionary[status.Side] = new List<UnitInfo>();

                    sightDictionary[status.Side].Add(info);
                }
            }
        }

        void SetStrongholdSightsClear()
        {
            foreach (var kvp in sightDictionary)
                kvp.Value.Clear();
        }
    }
}
