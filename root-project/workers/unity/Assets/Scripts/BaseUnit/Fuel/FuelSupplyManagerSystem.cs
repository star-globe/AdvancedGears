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
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FuelSupplyManagerSystem : BaseSearchSystem
    {
        EntityQuery group;

       ILogDispatcher logDispatcher;
        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = this.Origin;
            logDispatcher = this.LogDispatcher;

            group = GetEntityQuery(
                ComponentType.ReadWrite<FuelSupplyManager.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
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
            Entities.With(group).ForEach((ref FuelSupplyManager.Component manager,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (manager.FreeSupplyers.Count == 0)
                    return;

                var time = Time.time;
                var inter = manager.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                manager.Interval = inter;

                var feedList = new List<SupplyReserve>();
                var absorbList = new List<SupplyReserve>();

                foreach (var kvp in manager.SupplyPoints)
                {
                    FuelComponent.Component? comp = null;
                    if (TryGetComponent(kvp.Key, out comp) == false)
                        continue;

                    var fuel = comp.Value;
                    fuel.Fuel += kvp.Value.Reserve;

                    Func<SupplyReserve> func = () => new SupplyReserve(kvp.Value.Point, fuel.Fuel, fuel.MaxFuel);

                    var rate = fuel.FuelRate();
                    if (rate > checkRateUpper)
                        absorbList.Add(func());
                    else if (rate < checkRateUnder)
                        feedList.Add(func());
                }

                var emptyList = manager.FreeSupplyers;

                while (emptyList.Count > 0)
                {
                    var entity = emptyList[0];
                    if (MakeSupplyPlan(entity, absorbList, feedList, ref manager) == 0)
                        break;
                    emptyList.RemoveAt(0);
                }
            });
        }

        int MakeSupplyPlan(EntityId entityId, List<SupplyReserve> absorbList, List<SupplyReserve> feedList, ref FuelSupplyManager.Component manager)
        {
            FuelComponent.Component? comp = null;
            if (TryGetComponent(entityId, out comp) == false)
                return -1;

            var map = manager.SupplyPoints;
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
                var change = comp.Value.Fuel;
                feed.Fuel += change;
                UpdateReserve(feed.Point.StrongholdId, change, map);
            }

            if (plan.Orders.Count == 0)
                return 0;

            Unity.Entities.Entity entity;
            if (TryGetEntity(entityId, out entity) == false)
                return -1;

            this.CommandSystem.SendCommand(new FuelSupplyer.SetOrder.Request(entityId, plan.Orders[0]), entity);

            TargetInfo tgt;
            MakeTarget(plan.Orders[0], out tgt);
            this.CommandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(entityId, tgt), entity);

            manager.SupplyOrders.Add(entityId, plan);
            return 1;
        }

        void MakeTarget(in SupplyOrder order, out TargetInfo targetInfo)
        {
            targetInfo = new TargetInfo 
            {
                IsTarget = true,
                TargetId = order.Point.StrongholdId,
                Position = order.Point.Pos,
                Type = UnitType.Stronghold,
                Side = order.Point.Side,
                CommanderId = new EntityId(-1),
                AllyRange = 0.0f,
            };
        }

        SupplyReserve getNearestSupplyReserve(EntityId id, List<SupplyReserve> reserves)
        {
            Position.Component? comp = null;
            if (TryGetComponent(id, out comp) == false)
                return null;

            var pos = new Vector3f( (float)comp.Value.Coords.X,
                                    (float)comp.Value.Coords.Y,
                                    (float)comp.Value.Coords.Z);

            var length = float.MaxValue;
            SupplyReserve reserve = null;
            foreach(var r in reserves) {
                var diff = pos - r.Point.Pos;
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

            var detail = map[id];
            map[id] = new SupplyPointsDetail(detail.Point, detail.Reserve + change);
        }
    }
}
