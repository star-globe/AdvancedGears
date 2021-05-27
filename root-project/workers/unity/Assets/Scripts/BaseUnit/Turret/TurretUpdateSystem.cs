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
                                             ComponentType.ReadOnly<HexFacility.Component>(),
                                             ComponentType.ReadOnly<Transform>(),
                                             ComponentType.ReadOnly<SpatialEntityId>()
                                             ), period);
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
                                                      ref HexFacility.Component hex,
                                                      ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var units = getAllUnits(trans.position, HexDictionary.HexEdgeLength, allowDead:true, GetSingleUnitTypes(UnitType.Turret));

                var datas = turret.TurretsDatas;
                datas.Clear();

                var hexIndex = hex.HexIndex;
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
