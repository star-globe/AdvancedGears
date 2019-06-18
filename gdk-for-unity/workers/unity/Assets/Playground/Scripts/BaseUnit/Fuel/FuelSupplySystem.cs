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
    public class FuelSupplySystem : BaseSearchSystem
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
                ComponentType.Create<FuelSupplyer.Component>(),
                ComponentType.Create<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(FuelSupplyer.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var fuelSupplyer = group.GetComponentDataArray<FuelSupplyer.Component>();
            var fuelData = group.GetComponentDataArray<FuelComponent.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var targetData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < fuelSupplyer.Length; i++)
            {
                var supply = fuelSupplyer[i];
                var fuel = fuelData[i];
                var status = statusData[i];
                var tgt = targetData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Supply)
                    continue;

                bool is_enemy,allow_dead;
                switch(supply.Order.Type)
                {
                    case SupplyOrderType.Deliver:   is_enemy = false; allow_dead = true;    break;
                    case SupplyOrderType.Accept:    is_enemy = false; allow_dead = false;   break;
                    case SupplyOrderType.Occupy:    is_enemy = true; allow_dead = true;     break;

                    default:
                        continue;
                }

                var time = Time.realtimeSinceStartup;
                var inter = supply.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                supply.Interval = inter;

                float range = supply.Range;
                int current = fuel.Fuel;

                var id = supply.Order.StrongholdId;
                var unit = getUnits(status.Side, pos, range, is_enemy, allow_dead, UnitType.Stronghold).FirstOrDefault(u => u.id == id);
                if (unit != null) {
                    // todo
                    DealOrder(unit, FuelModifyType.Feed, 100, ref current);
                }

                if (fuel.Fuel != current)
                {
                    fuel.Fuel = current;
                    fuelData[i] = fuel;
                }
                
                fuelSupplyer[i] = supply;
            }
        }

        void DealOrder(UnitInfo unit, FuelModifyType type, int feed, ref int current)
        {
            FuelComponent.Component? comp = null;
            if (TryGetComponent(unit.id, out comp) == false)
                return;

            var f = comp.Value.Fuel;
            var max = comp.Value.MaxFuel;
            if (f >= max)
                return;

            int num = 0;
            switch(type) {
                case FuelModifyType.Feed: num = Mathf.Clamp(max - f, 0, feed); break;
                // todo
                default: return;
            }

            if (current < num)
                return;
            
            current -= num;
            var modify = new FuelModifier
            {
                Type = type,
                Amount = num,
            };
            updateSystem.SendEvent(new FuelComponent.FuelModified.Event(modify), unit.id);
        }
    }
}
