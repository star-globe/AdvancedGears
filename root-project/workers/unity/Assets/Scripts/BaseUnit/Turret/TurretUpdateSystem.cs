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
        private EntityQuerySet hubQuerySet;

        const int period = 2;
        protected override void OnCreate()
        {
            base.OnCreate();

            hubQuerySet = new EntityQuerySet(GetEntityQuery(
                                             ComponentType.ReadWrite<TurretHub.Component>(),
                                             ComponentType.ReadOnly<TurretHub.HasAuthority>(),
                                             ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                             ComponentType.ReadOnly<StrategyHexAccessPortal.Component>(),
                                             ComponentType.ReadOnly<Transform>(),
                                             ComponentType.ReadOnly<SpatialEntityId>()
                                             ), period);

            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            UpdateTurretHubData();
        }

        private void UpdateTurretHubData()
        {
            if (CheckTime(ref hubQuerySet.inter) == false)
                return;

            Entities.With(hubQuerySet.group).ForEach((Entity entity,
                                                      ref TurretHub.Component turret,
                                                      ref BaseUnitStatus.Component status,
                                                      ref StrategyHexAccessPortal.Component portal,
                                                      ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var units = getAllyUnits(status.Side, trans.position, HexDictionary.HexEdgeLength, allowDead:true, UnitType.Turret);

                var datas = turret.TurretsDatas;
                datas.Clear();

                var hexIndex = portal.Index;
                foreach(var u in units) {
                    if (hexIndex != uint.MaxValue && HexUtils.IsInsideHex(this.Origin, hexIndex, u.pos, HexDictionary.HexEdgeLength) == false)
                        continue;

                    if (TryGetComponent<TurretComponent.Component>(u.id, out var comp) == false)
                        continue;

                    datas[u.id] = new TurretInfo(u.side, comp.Value.MasterId, u.id);
                }

                turret.TurretsDatas = datas;
            });
        }
    }
}
