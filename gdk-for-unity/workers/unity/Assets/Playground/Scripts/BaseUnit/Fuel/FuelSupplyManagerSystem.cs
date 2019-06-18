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
    public class FuelSupplyManagerSystem : BaseSearchSystem
    {
        ComponentGroup group;
        CommandSystem commandSystem;
        ComponentUpdateSystem updateSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingManager<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingManager<CommandSystem>();
            updateSystem = World.GetExistingManager<ComponentUpdateSystem>();
            group = GetComponentGroup(
                ComponentType.Create<FuelServer.Component>(),
                ComponentType.Create<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(FuelServer.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var fuelServer = group.GetComponentDataArray<FuelServer.Component>();
            var fuelData = group.GetComponentDataArray<FuelComponent.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < fuelServer.Length; i++)
            {
                var server = fuelServer[i];
                var fuel = fuelData[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Stronghold)
                    continue;

                if (fuel.Fuel == 0)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = server.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                server.Interval = inter;

                float range = server.Range;
                int baseFeed = server.FeedRate;
                int current = fuel.Fuel;
                current += server.GainRate;

                var list = getUnits(status.Side, pos, range, false, UnitType.Soldier, UnitType.Commander);
                foreach(var unit in list)
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
                {
                    fuel.Fuel = current;
                    fuelData[i] = fuel;
                }
                
                fuelServer[i] = server;
            }
        }
    }
}
