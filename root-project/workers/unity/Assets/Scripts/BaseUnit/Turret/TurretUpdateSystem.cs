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
        private EntityQueryBuilder.F_CDDDD<Transform, TurretHub.Component, BaseUnitStatus.Component, HexFacility.Component, SpatialEntityId> action;
        const int period = 2;

        readonly HashSet<EntityId> removeKeys = new HashSet<EntityId>();

        protected override void OnCreate()
        {
            base.OnCreate();

            hubQuerySet = new EntityQuerySet(GetEntityQuery(
                                             ComponentType.ReadWrite<TurretHub.Component>(),
                                             ComponentType.ReadOnly<TurretHub.HasAuthority>(),
                                             ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                             ComponentType.ReadOnly<HexFacility.Component>(),
                                             ComponentType.ReadOnly<Transform>()
                                             ), period);
            action = Query;
        }

        protected override void OnUpdate()
        {
            UpdateTurretHubData();
        }

        private void UpdateTurretHubData()
        {
            if (CheckTime(ref hubQuerySet.inter) == false)
                return;

            Entities.With(hubQuerySet.group).ForEach(action);
        }

        private void Query(Transform trans,
                            ref TurretHub.Component turret,
                            ref BaseUnitStatus.Component status,
                            ref HexFacility.Component hex,
                            ref SpatialEntityId entityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (UnitUtils.IsBuilding(status.Type) == false)
                return;

            var datas = turret.TurretsDatas;

            if (IsNeedRenewTurrets(datas) == false)
                return;

            var units = getAllUnits(trans.position, HexDictionary.HexEdgeLength, allowDead:true, GetSingleUnitTypes(UnitType.Turret));

            removeKeys.Clear();
            foreach (var k in datas.Keys)
                removeKeys.Add(k);

            bool changed = false;
           
            var hexIndex = hex.HexIndex;
            foreach(var u in units) {
                if (hexIndex != uint.MaxValue && HexUtils.IsInsideHex(this.Origin, hexIndex, u.pos, HexDictionary.HexEdgeLength) == false)
                    continue;

                if (TryGetComponent<TurretComponent.Component>(u.id, out var comp) == false)
                    continue;

                int masterId = comp.Value.MasterId;

                if (datas.ContainsKey(u.id)) {
                    var val = datas[u.id];
                    if (CheckDiffTurretInfo(ref val, u.side, masterId, u.id)) {
                        datas[u.id] = val;
                        changed = true;
                    }

                    removeKeys.Remove(u.id);
                }
                else {
                    datas[u.id] = new TurretInfo(u.side, masterId, u.id);
                    changed = true;
                }
            }

            if (removeKeys.Count == 0 && changed == false)
                return;

            foreach (var k in removeKeys)
                datas.Remove(k);

            turret.TurretsDatas = datas;
        }

        private bool CheckDiffTurretInfo(ref TurretInfo info, UnitSide side, int masterId, EntityId uid)
        {
            bool isChanged = false;

            if (info.Side != side) {
                info.Side = side;
                isChanged = true;
            }

            if (info.MasterId != masterId) {
                info.MasterId = masterId;
                isChanged = true;
            }

            if (info.EntityId != uid) {
                info.EntityId = uid;
                isChanged = true;
            }

            return isChanged;
        }

        private bool IsNeedRenewTurrets(Dictionary<EntityId,TurretInfo> turretsDic)
        {
            if (turretsDic.Count == 0)
                return true;

            foreach (var kvp in turretsDic) {
                var uid = kvp.Key;
                if (TryGetComponent<BaseUnitStatus.Component>(uid, out var status) == false ||
                    TryGetComponent<TurretComponent.Component>(uid, out var turret) == false)
                    return true;

                var val = kvp.Value;
                if (CheckDiffTurretInfo(ref val, status.Value.Side, turret.Value.MasterId, uid))
                    return true;
            }

            return false;
        }
    }
}
