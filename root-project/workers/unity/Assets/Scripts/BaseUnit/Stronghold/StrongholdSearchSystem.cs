using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class StrongholdSearchSystem : BaseSearchSystem
    {
        private EntityQuery group;
        IntervalChecker inter;

        const int period = 2;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrongholdSight.Component>(),
                ComponentType.ReadOnly<StrongholdSight.ComponentAuthority>(),
                ComponentType.ReadWrite<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.ComponentAuthority>(),
                ComponentType.ReadOnly<StrongholdStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(StrongholdSight.ComponentAuthority.Authoritative);
            group.SetFilter(BaseUnitStatus.ComponentAuthority.Authoritative);

            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref StrongholdSight.Component sight,
                                          ref StrongholdStatus.Component stronghold,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var inter = sight.Interval;
                if (inter.CheckTime() == false)
                    return;

                sight.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);

                var targets = sight.TargetStrongholds;
                var vector = sight.StrategyVector.Vector.ToUnityVector();
                var order = GetTargetStronghold(trans.position, status.Side, vector, entityId.EntityId, targets);

                sight.TargetStrongholds = targets;
                status.Order = order;
            });
        }

        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, in Vector3 vector, EntityId selfId, Dictionary<EntityId,TargetStrongholdInfo> targets)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector;// * RangeDictionary.StrategyRangeRate;
            var range = strategyVector.magnitude;
            var units = getEnemyUnits(side, pos, range, allowDead:true, UnitType.Stronghold);
            if (units != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                units = getAllyUnits(side, newCenter, range, allowDead:true, UnitType.Stronghold);
                if (units != null)
                    units.RemoveAll(u => u.id == selfId);

                if (units != null || units.Count > 0)
                    order = OrderType.Supply;
            }

            targets.Clear();
            if (units != null && units.Count > 0) {
                foreach (var u in units) {
                    targets.Add(u.id, new TargetStrongholdInfo(u.id, u.side, u.pos.ToWorldPosition(this.Origin).ToCoordinates()));
                }
            }
            else {
                targets.Add(selfId, new TargetStrongholdInfo(selfId, side, pos.ToWorldPosition(this.Origin).ToCoordinates()));
                order = OrderType.Keep;
            }

            //Debug.LogFormat("Side:{0} Order:{1} StrategyVector:{2}", side, order, strategyVector);

            return order;
        }
    }
}
