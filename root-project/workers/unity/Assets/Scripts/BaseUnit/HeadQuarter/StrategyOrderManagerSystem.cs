using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
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
    class StrategyOrderManagerSystem : BaseSearchSystem
    {
        private EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrategyOrderManager.Component>(),
                ComponentType.ReadOnly<StrategyOrderManager.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(StrategyOrderManager.ComponentAuthority.Authoritative);

            inter = IntervalCheckerInitializer.InitializedChecker(10.0f);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
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
                if (inter.CheckTime() == false)
                    return;

                manager.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var range = RangeDictionary.Get(FixedRangeType.HeadQuarterRange);

                var entityId = new EntityId();
                var enemy = getNearestEnemey(status.Side, trans.position, range, UnitType.HeadQuarter);
                if (enemy != null)
                    entityId = enemy.id;

                var target = manager.TargetHq;
                if (target.HeadQuarterId == entityId)
                    return;

                manager.TargetHq = new TargetHeadQuartersInfo()
                {
                    HeadQuarterId = enemy.id,
                    Side = enemy.side,
                    Position = enemy.pos.ToCoordinates(),
                };

                var allies = getAllyUnits(status.Side, trans.position, range, UnitType.Stronghold);
                foreach(var a in allies) {
                    
                }
            });
        }
    }
}
