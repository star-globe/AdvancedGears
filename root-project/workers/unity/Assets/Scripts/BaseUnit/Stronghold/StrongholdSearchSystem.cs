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
        }

        protected override void OnUpdate()
        {
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

                var target = sight.TargetStronghold;
                var vector = sight.StrategyVector.Vector.ToUnityVector();
                var order = GetTargetStronghold(trans.position, status.Side, vector, entityId.EntityId, ref target);

                sight.TargetStronghold = target;
                status.Order = order;
            });
        }

        const float vectorRate = 100.0f;
        private OrderType GetTargetStronghold(in Vector3 pos, UnitSide side, in Vector3 vector, EntityId selfId, ref TargetStrongholdInfo target)
        {
            OrderType order = OrderType.Idle;

            var strategyVector = vector * vectorRate;
            var range = strategyVector.magnitude;
            var unit = getNearestEnemey(side, pos, range, UnitType.Stronghold);
            if (unit != null) {
                order = OrderType.Attack;
            }
            else {
                var newCenter = pos + strategyVector;
                unit = getNearestAlly(selfId, side, newCenter, range, UnitType.Stronghold);
                if (unit != null)
                    order = OrderType.Supply;
            }

            if (unit != null) {
                target.StrongholdId = unit.id;
                target.Side = unit.side;
                target.Position = unit.pos.ToWorldPosition(this.Origin).ToCoordinates();
            }
            else {
                target.StrongholdId = selfId;
                target.Side = side;
                target.Position = pos.ToWorldPosition(this.Origin).ToCoordinates();
                order = OrderType.Keep;
            }

            Debug.LogFormat("Side:{0} Order:{1} StrategyVector:{2}", side, order, strategyVector);

            return order;
        }
    }
}
