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
                ComponentType.Create<FuelSupplyManager.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(FuelSupplyManager.ComponentAuthority.Authoritative);
        }

        private class SupplyReserve
        {
            public SupplyPoint Point { get; private set; }
            int fuel;
            public int Fuel
            {
                get { return fuel; }
                set { fuel = Mathf.Clamp(value, 0, Max); }
            }
            public int Max { get; private set; }

            public float Rate { get { return fuel * 1.0f / Max; } }

            public SupplyReserve(SupplyPoint point, int fuel, int max) {
                this.Point = point;
                this.Fuel = fuel;
                this.Max = max;
            }
        }

        const float checkRateUpper = 0.7f;
        const float checkRateUnder = 0.4f;

        protected override void OnUpdate()
        {
            var fuelManager = group.GetComponentDataArray<FuelSupplyManager.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < fuelManager.Length; i++) {
                var manager = fuelManager[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Stronghold)
                    continue;

                if (manager.FreeSupplyers.Count == 0)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = manager.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                manager.Interval = inter;

                var feedList = new List<SupplyReserve>();
                var absorbList = new List<SupplyReserve>();

                foreach(var kvp in manager.SupplyPoints) {
                    FuelComponent.Component? comp = null;
                    if (TryGetComponent(kvp.Key, out comp) == false)
                        continue;

                    var fuel = comp.Value;
                    fuel.Fuel += kvp.Value.Reserve;

                    Func<SupplyReserve> func = ()=> new SupplyReserve(kvp.Value.Point, fuel.Fuel, fuel.MaxFuel);

                    var rate = fuel.FuelRate();
                    if (rate > checkRateUpper)
                        absorbList.Add(func());
                    else if (rate < checkRateUnder)
                        feedList.Add(func());
                }

                var emptyList = manager.FreeSupplyers;

                while(emptyList.Count > 0) {
                    var entity = emptyList[0];
                    if (MakeSupplyPlan(entity, absorbList, feedList, manager.SupplyPoints) == 0)
                        break;
                    emptyList.RemoveAt(0);
                }

                fuelManager[i] = manager;
            }
        }

        int MakeSupplyPlan(EntityId entityId, List<EntityId> absorbList, List<EntityId> feedList, Dictionary<EntityId,SupplyPointsDetail> map)
        {
            FuelComponent.Component? comp = null;
            if (TryGetComponent(entityId, out comp) == false)
                return -1;

            var plan = new SupplyPlan { Orders = new List<SupplyOrder>() };

            var rate = comp.Value.FuelRate();
            if (rate <= checkRateUpper) {
                var abs = getNearestSupplyReserve(entityId, absorbList);
                if (abs != null && abs.Rate > checkRateUpper) {
                    plan.Orders.Add(new SupplyOrder { Type = SupplyOrderType.Accept, Point = abs.Point });
                    var change = -comp.Value.EmptyCapacity();
                    abs.Fuel += change;
                    UpdateReserve(abs.Point.StrongholdId, change, map);
                }
            }

            var feed = getNearestSupplyReserve(entityId, feedList);
            if (feed != null && feed.Rate <= checkRateUpper) {
                plan.Orders.Add(new SupplyOrder { Type = SupplyOrderType.Deliver, Point = feed.Point });
                var change = comp.Value.FuelRate;
                feed.Fuel += change;
                UpdateReserve(feed.Point.StrongholdId, change, map);
            }

            if (plan.Orders.Count == 0)
                return 0;

            Entity entity;
            if (TryGetEntity(entityId, out entity) == false)
                return -1;

            commandSystem.SendCommand(new FuelSupplyer.SetOrder.Request(entityId, plan[0]), entity);

            map.Add(entityId, plan);
            return 1;
        }

        SupplyReserve getNearestSupplyReserve(EntityId id, List<SupplyReserve> reserves)
        {
            Position.Componet? comp = null;
            if (TryGetComponent(id, out comp) == false)
                return null;

            var pos = new Vector3f( (float)comp.Value.Coords.X,
                                    (float)comp.Value.Coords.Y,
                                    (float)comp.Value.Coords.Z);

            var length = float.MaxValue;
            SupplyReserve reserve = null;
            foreach(var r in reserves) {
                var diff = pos - r.point.Pos;
                var l = diff.SqrMagnitude();
                if (l >= length)
                    continue;

                length = l;
                reserve = r;
            }

            return reserve;
        }

        void UpdateReserve(EntityId id, int change, Dictionary<EntityId,SupplyPointsDetail> map)
        {
            if (map.ContainsKey(id) == false) 
                return;

            map[id].Reserve += change;
        }
    }
}
