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
    public class UnitFactorySystem : ComponentSystem
    {
        ComponentGroup group;
        CommandSystem commandSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        private class ProductOrderCotext
        {
            public FollowerOrder? f_order;
            public SuperiorOrder? s_order;
            public UnitType type;
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingManager<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingManager<CommandSystem>();
            group = GetComponentGroup(
                ComponentType.Create<UnitFactory.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            HandleProductUnit();
            HandleProductResponse();
        }

        // TODO:getFromSettings;
        const float timeCost = 5;
        void HandleProductUnit()
        {
            var factoryData = group.GetComponentDataArray<UnitFactory.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < factoryData.Length; i++) {
                var factory = factoryData[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                FollowerOrder? f_order = null;
                SuperiorOrder? s_order = null;

                if (factory.SuperiorOrders.Count > 0)
                    s_order = factory.SuperiorOrders[0];
                else if (factory.FollowerOrders.Count > 0)
                    f_order = factory.FollowerOrders[0];

                // calc time cost
                float cost = 0.0f;
                if (s_order == null && f_order == null)
                {
                    factoryData[i] = factory;
                    continue;
                }

                var time = Time.realtimeSinceStartup;
                var inter = factory.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                factory.Interval = inter;

                cost = timeCost;
                if (factory.CurrentType == UnitType.None) {
                    factory.ProductInterval = new IntervalChecker(cost, time + cost);
                    factory.CurrentType = s_order != null ? UnitType.Commander: f_order.Value.Type;
                }

                inter = factory.ProductInterval;
                if (inter.CheckTime(time)) {
                    EntityTemplate template = null;
                    var coords = new Coordinates(pos.x, pos.y, pos.z);

                    bool finished = false;
                    if (s_order != null)
                        template = CreateSuperior(factory.SuperiorOrders, coords, out finished);
                    else if (f_order != null)
                        template = CreateFollower(factory.FollowerOrders, coords, out finished);

                    var request = new WorldCommands.CreateEntity.Request
                    (
                        template,
                        context: new ProductOrderCotext() { f_order = f_order, s_order = s_order, type = factory.CurrentType }
                    );
                   commandSystem.SendCommand(request);

                    if (finished)
                        factory.CurrentType = UnitType.None;
                }

                factory.ProductInterval = inter;
                factoryData[i] = factory;
            }
        }

        EntityTemplate CreateFollower(List<FollowerOrder> orders, in Coordinates coords, out bool finished)
        {
            finished = false;
            if (orders.Count == 0)
                return null;

            var current = orders[0];
            // create unit
            EntityTemplate template = null;
            switch (current.Type)
            {
                case UnitType.Commander:
                    template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, coords, current.Rank);
                    break;
                default:
                    template = BaseUnitTemplate.CreateBaseUnitEntityTemplate(current.Side, coords, current.Type);
                    break;
            }

            current.Number--;
            if (current.Number <= 0) {
                orders.RemoveAt(0);
                finished = true;
            }

            return template;
        }

        EntityTemplate CreateSuperior(List<SuperiorOrder> orders, in Coordinates coords, out bool finished)
        {
            finished = false;
            if (orders.Count == 0)
                return null;

            var current = orders[0];
            // create unit
            EntityTemplate template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, coords, current.Rank);
            var snap = template.GetComponent<CommanderStatus.Snapshot>();
            if (snap != null) {
                var s = snap.Value;
                s.FollowerInfo.Followers.AddRange(current.Followers);
                template.SetComponent(s);
            }

            orders.RemoveAt(0);
            finished = true;

            return template;
        }

        void HandleProductResponse()
        {
            var followerDic = new Dictionary<EntityId,FollowerInfo>();
            var superiorDic = new Dictionary<EntityId,List<EntityId>>();

            var responses = commandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++)
            {
                ref readonly var response = ref responses[i];
                if (!(response.Context is ProductOrderCotext requestContext))
                {
                    // Ignore non-player entity creation requests
                    continue;
                }

                if (response.StatusCode != StatusCode.Success)
                    continue;

                var order = requestContext;
                if (order.f_order != null) {
                    var id = order.f_order.Value.Customer;
                    if (followerDic.ContainsKey(id) == false)
                        followerDic.Add(id, new FollowerInfo { Followers = new List<EntityId>(),
                                                               UnderCommanders = new List<EntityId>() });
                    var info = followerDic[id];
                    var entityId = response.EntityId.Value;
                    switch (order.type)
                    {
                        case UnitType.Soldier:      info.Followers.Add(entityId); break;
                        case UnitType.Commander:    info.UnderCommanders.Add(entityId); break;
                    }
                }

                if (order.s_order != null) {
                    var id = response.EntityId.Value;
                    if (superiorDic.ContainsKey(id) == false)
                        superiorDic.Add(id, new List<EntityId>());
                    var list = superiorDic[id];
                    list.AddRange(order.s_order.Value.Followers);
                }
            }

            // SetFollowers
            foreach(var kvp in followerDic) {
                var info = kvp.Value;
                commandSystem.SendCommand(new CommanderStatus.AddFollower.Request(kvp.Key, new FollowerInfo { Followers = info.Followers.ToList(),
                                                                                                              UnderCommanders = info.UnderCommanders.ToList() }));
            }

            // SetSuperiors
            foreach(var kvp in superiorDic) {
                foreach(var f in kvp.Value) {
                    commandSystem.SendCommand(new CommanderStatus.SetSuperior.Request(f, new SuperiorInfo { EntityId = kvp.Key }));
                }
            }
        }
    }
}
