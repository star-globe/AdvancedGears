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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class TurretUpdateSystem : BaseSearchSystem
    {
        private EntityQuery group;
        IntervalChecker inter;

        const int period = 2;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<TurretHub.Component>(),
                ComponentType.ReadOnly<TurretHub.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref TurretHub.Component turret,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var units = getAllyUnits(status.Side, trans.position, RangeDictionary.Get(FixedRangeType.StrongholdRange), UnitType.Turret);

                var datas = turret.TurretsDatas;
                datas.Clear();

                foreach(var u in units) {
                    if (TryGetComponent<TurretComponent.Component>(u.id, out var comp) == false)
                        continue;

                    datas[u.id] = new TurretInfo(u.side, comp.Value.MasterId, u.id);
                }

                turret.TurretsDatas = datas;
            });
        }
    }
}
