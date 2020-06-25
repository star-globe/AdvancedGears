using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class HealthRecoveryInHexSystem : HexUpdateBaseSystem
    {
        EntityQuery unitGroup;
        IntervalChecker inter;
        const int frequency = 1; 

        protected override void OnCreate()
        {
            base.OnCreate();

            unitGroup = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitHealth.Component>(),
                ComponentType.ReadOnly<BaseUnitHealth.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f / frequency);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            UpdateRecovery();
        }

        private void UpdateRecovery()
        {
            if (CheckTime(ref inter) == false)
                return;

            var interval = 1.0f;

            Entities.With(unitGroup).ForEach((Entity entity,
                                      ref BaseUnitHealth.Component health,
                                      ref BaseUnitStatus.Component status,
                                      ref SpatialEntityId entityId) =>
            {
                if (status.State == UnitState.Dead)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                int? hexMasterId = null;
                foreach(var  kvp in base.hexDic) {
                    if (kvp.Value.Side != status.Side)
                        continue;

                    if (HexUtils.IsInsideHex(this.Origin, kvp.Key, pos, HexDictionary.HexEdgeLength)) {
                        hexMasterId = kvp.Value.HexId;
                        break;
                    }
                }

                if (hexMasterId == null)
                    return;

                if (health.Health >= health.MaxHealth)
                    return;

                // rate from hexMasterId
                var rate = 1.0f/ 100;
                var amount = health.RecoveryAmount;
                amount += rate * interval;

                var floor = Mathf.FloorToInt(amount);
                if (floor > 0) {
                    health.Health = Mathf.Min(health.Health + floor, health.MaxHealth);
                    amount -= floor;
                }

                health.RecoveryAmount = amount;
            });
        }
    }
}
