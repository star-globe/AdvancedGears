using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    public class FuelServerSystem : BaseSearchSystem
    {
        EntityQuery group;
        CommandSystem commandSystem;
        ComponentUpdateSystem updateSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingSystem<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingSystem<CommandSystem>();
            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            group = GetEntityQuery(
                ComponentType.ReadWrite<FuelServer.Component>(),
                ComponentType.ReadWrite<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(FuelServer.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

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

                var time = Time.realtimeSinceStartup;
                var inter = server.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                server.Interval = inter;

                float range = server.Range;
                int baseFeed = server.FeedRate;
                int current = fuel.Fuel;
                current += server.GainRate;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var list = getUnits(status.Side, pos, range, false, false, UnitType.Soldier, UnitType.Commander);
                foreach (var unit in list)
                {
                    FuelComponent.Component? comp = null;
                    if (TryGetComponent(unit.id, out comp))
                    {
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
                        updateSystem.SendEvent(new FuelComponent.FuelModified.Event(modify), unit.id);
                    }
                }

                if (fuel.Fuel != current)
                    fuel.Fuel = current;
            });
        }
    }
}
