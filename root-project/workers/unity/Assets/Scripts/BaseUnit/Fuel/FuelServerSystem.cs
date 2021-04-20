using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [Obsolete]
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FuelServerSystem : BaseSearchSystem
    {
        EntityQuery group;
        UnitType[] fuelTypes = null;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<FuelServer.Component>(),
                ComponentType.ReadWrite<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            fuelTypes = new UnitType[] { UnitType.Soldier, UnitType.Commander };
        }

        const int rate = 2;
        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref FuelServer.Component server,
                                          ref FuelComponent.Component fuel,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (fuel.Fuel == 0)
                    return;

                var inter = server.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                server.Interval = inter;

                float range = server.Range;
                int baseFeed = server.FeedRate;
                int current = fuel.Fuel;
                current += server.GainRate;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var list = getAllyUnits(status.Side, pos, range, allowDead:false, fuelTypes);
                foreach (var unit in list)
                {
                    FuelComponent.Component? comp = null;
                    if (TryGetComponent(unit.id, out comp)) {
                        var f = comp.Value.Fuel;
                        var max = comp.Value.MaxFuel;
                        if (f >= max)
                            continue;

                        var num = Mathf.Clamp(max - f, 0, baseFeed);
                        if (current < num)
                            continue;

                        current -= num;

                        var modify = new FuelModifier
                        {
                            Type = FuelModifyType.Feed,
                            Amount = num,
                        };
                        this.UpdateSystem.SendEvent(new FuelComponent.FuelModified.Event(modify), unit.id);
                    }

                    GunComponent.Component? gun = null;
                    if (TryGetComponent(unit.id, out gun)) {
                        var dic = gun.Value.GunsDic;
                        foreach(var kvp in dic) {
                            var info = kvp.Value;
                            var b = info.StockBullets;
                            var max = info.StockMax;
                            if (b >= max)
                                continue;
                            
                            var feed = baseFeed / rate;
                            var num = Mathf.Clamp(max - b, 0, feed);
                            if (current < num * feed)
                                continue;

                            current -= num * feed;
                            var supply = new SupplyBulletInfo {
                                GunId = info.GunId,
                                GunTypeId = info.GunTypeId,
                                AttachedBone = kvp.Key,
                                Amount = num,
                            };
                            this.UpdateSystem.SendEvent(new GunComponent.BulletSupplied.Event(supply), unit.id);
                            break;
                        }
                    }
                }

                if (fuel.Fuel != current)
                    fuel.Fuel = current;
            });
        }
    }
}
