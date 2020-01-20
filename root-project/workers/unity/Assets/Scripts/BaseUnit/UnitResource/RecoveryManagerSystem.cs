using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class RecoveryManagerSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;
        const float time = 1.0f; 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<RecoveryComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(RecoveryComponent.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(time);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref RecoveryComponent.Component recovery,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (status.Side == UnitSide.None)
                    return;

                var current = Time.time;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                switch (recovery.State)
                {
                    case RecoveryState.Supplying:
                        RecoveryUnits(status.Side, pos, current, ref recovery);
                        break;

                    case RecoveryState.Reducing:
                        RecoveryUnits(status.Side, pos, current, ref recovery, isReducing:true);
                        break;

                    default:
                        return;
                }
            });
        }

        private void RecoveryUnits(UnitSide side, in Vector3 pos, float current, ref RecoveryComponent.Component recovery, bool isReducing = false)
        {
            if (recovery.CheckedTime != 0.0f) {
                var delta = current - recovery.CheckedTime;

                var allies = getAllyUnits(side, pos, recovery.Range);
                foreach(var unit in allies) {
                    BaseUnitHealth.Component? health;
                    if (this.TryGetComponent(unit.id, out health) && health.Value.IsPoor()) {
                        var diff = (int)(health.Value.MaxHealth * recovery.RecoveryRate * delta);
                        if (diff > 0)
                            this.UpdateSystem.SendEvent(new BaseUnitHealth.HealthDiffed.Event(new HealthDiff {Diff = diff }), unit.id);
                    }

                    FuelComponent.Component? fuel;
                    if (this.TryGetComponent(unit.id, out fuel) && fuel.Value.IsPoor()) {
                        var diff = (int)(fuel.Value.MaxFuel * recovery.RecoveryRate * delta);
                        if (diff > 0)
                            this.UpdateSystem.SendEvent(new FuelComponent.FuelDiffed.Event(new FuelDiff { Diff = diff }), unit.id);
                    }
                }
            }

            if (isReducing && current > recovery.EndTime)
                recovery.State = RecoveryState.Stopped;

            recovery.CheckedTime = current;
        }
    }
}
