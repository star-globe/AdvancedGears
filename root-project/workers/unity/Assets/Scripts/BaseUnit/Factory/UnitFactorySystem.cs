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
    public class UnitFactorySystem : SpatialComponentSystem
    {
        EntityQuery group;

        private class ProductOrderContext
        {
            public FollowerOrder? f_order;
            public SuperiorOrder? s_order;
            public UnitType type;
            public uint CommanderRank
            {
                get
                {
                    if (f_order != null)
                        return f_order.Value.Rank;

                    return s_order.Value.Rank;
                }
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(UnitFactory.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleProductUnit();
            HandleProductResponse();
        }

        // TODO:getFromSettings;
        const float timeCost = 2;
        const float range = 12.0f;
        const float height_buffer = 5.0f;
        void HandleProductUnit()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref UnitFactory.Component factory,
                                          ref BaseUnitStatus.Component status,
                                          ref StrongholdStatus.Component stronghold,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                FollowerOrder? f_order = null;
                SuperiorOrder? s_order = null;
                TeamOrder? t_order = null;

                FactoryOrderType orderType = FactoryOrderType.None;

                if (factory.SuperiorOrders.Count > 0) {
                    s_order = factory.SuperiorOrders[0];
                    orderType = FactoryOrderType.Superior;
                }
                else if (factory.FollowerOrders.Count > 0) {
                    f_order = factory.FollowerOrders[0];
                    orderType = FactoryOrderType.Follower;
                }
                else if (factory.TeamOrders.Count > 0) {
                    t_order = factory.TeamOrders[0];
                    orderType = FactoryOrderType.Team;
                }

                // calc time cost
                float cost = 0.0f;
                if (orderType == FactoryOrderType.None)
                    return;

                var time = Time.time;
                var inter = factory.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                factory.Interval = inter;

                cost = timeCost;
                if (factory.CurrentType == FactoryOrderType.None)
                {
                    factory.ProductInterval = new IntervalChecker(cost, time + cost, 0, -1);   // TODO modify
                    factory.CurrentType = orderType;
                }

                inter = factory.ProductInterval;
                if (inter.CheckTime(time) == false)
                    return;

                factory.ProductInterval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var p = trans.position + RandomLogic.XZRandomCirclePos(range);

                var pos = p - this.Origin;
                EntityTemplate template = null;
                var coords = new Coordinates(pos.x, pos.y + height_buffer, pos.z);

                bool finished = false;
                if (s_order != null)
                    template = CreateSuperior(factory.SuperiorOrders, coords, out finished);
                else if (f_order != null)
                    template = CreateFollower(factory.FollowerOrders, coords, f_order.Value.Customer, out finished);

                if (template != null) {
                    var request = new WorldCommands.CreateEntity.Request
                    (
                        template,
                        context: new ProductOrderContext() { f_order = f_order,
                                                             s_order = s_order,
                                                             type = factory.CurrentType }
                    );
                    this.CommandSystem.SendCommand(request);
                }
                else {

                }

                if (finished)
                    factory.CurrentType = FactoryOrderType.None;
            });
        }

        EntityTemplate CreateFollower(List<FollowerOrder> orders, in Coordinates coords, in EntityId superiorId, out bool finished)
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
                    template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, coords, current.Rank, superiorId);
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
            else
            {
                orders[0] = current;
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
            EntityTemplate template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, coords, current.Rank, null);
            var snap = template.GetComponent<CommanderTeam.Snapshot>();
            if (snap != null) {
                var s = snap.Value;
                s.FollowerInfo.Followers.AddRange(current.Followers);
                template.SetComponent(s);
            }

            orders.RemoveAt(0);
            finished = true;

            return template;
        }

        class RequestInfo
        {
            public TeamInfo team;
            public List<EntityId> soldiers;
        }

        readonly Dictionary<EntityId,Dictionary<int,RequestInfo>> requestDic = new Dictionary<EntityId, Dictionary<int, RequestInfo>>();
        int currentRequestId = 0;

        private class TeamOrderContext
        {
            public UnitType type;
            public int requestId; 
        }

        void CreateTeam(UnitSide side, EntityId id, in TeamOrder team, in Coordinates coords, out bool fnished)
        {
            List<ValueTuple<EntityTemplate,UnitType>> templates = new List<ValueTuple<EntityTemplate,UnitType>>();
            var temp = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(side, coords, team.CommanderRank);
            templates.Add((temp, UnitType.Commander));
            foreach(var i in Enumrable.Range(0,team.Stack)) {
                temp = BaseUnitTemplate.CreateBaseUnitEntityTemplate(side, coords, UnitType.Soldier);
                templates.Add((temp, UnitType.Soldier));
            }

            foreach(var pair in templates) {
                this.CommandSystem.SendCommand(new WorldCommands.CreateEntity.Request(
                    pair.item1,
                    context: new TeamOrderContext() { type = pair.Item2, 
                                                      requestId = currentRequestId }
                ));
            }

            Dictionary<int, RequestInfo> dic = null;
            if (requestDic.TryGetValue(id, out dic) == false)
                dic = new Dictionary<int, RequestInfo>();

            dic.Add(currentRequestId, new RequestInfo()
            {
                team = new TeamInfo() { CommanderId = Entity.Nul,
                                        Rank = team.CommanderRank,
                                        Order = team.Order,
                                        TargetEntityId = Entity.Null },
                soldiers = new List<EntityId>(),
            });

            currentRequestId++;
        }

        void HandleProductResponse()
        {
            Dictionary<EntityId, FollowerInfo> followerDic = null;
            Dictionary<EntityId, List<EntityId>> superiorDic = null;
            Dictionary<EntityId, List<CommanderInfo>> commanders = null;

            var responses = this.CommandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (!(response.Context is ProductOrderContext requestContext)) {
                    // Ignore non-player entity creation requests
                    continue;
                }

                if (response.StatusCode != StatusCode.Success)
                    continue;

                var order = requestContext;
                if (order.f_order != null) {
                    followerDic = followerDic ?? new Dictionary<EntityId, FollowerInfo>();

                    var id = order.f_order.Value.Customer;
                    if (followerDic.ContainsKey(id) == false)
                        followerDic.Add(id, new FollowerInfo { Followers = new List<EntityId>(),
                                                               UnderCommanders = new List<EntityId>() });

                    var info = followerDic[id];
                    var entityId = response.EntityId.Value;

                    switch (order.type)
                    {
                        case UnitType.Soldier:
                            info.Followers.Add(entityId);
                            break;

                        case UnitType.Commander:
                            info.UnderCommanders.Add(entityId);
                            //SetList(ref commanders, entityId, order.CommanderRank);
                            break;
                    }

                    followerDic[id] = info;
                }

                if (order.s_order != null) {
                    var id = response.EntityId.Value;
                    SetList(ref superiorDic, id, order.s_order.Value.Followers);
                    //SetList(ref commanders, id, order.CommanderRank);
                }
            }

            // SetFollowers
            if (followerDic != null) {
                foreach (var kvp in followerDic)
                {
                    var info = kvp.Value;
                    this.CommandSystem.SendCommand(new CommanderTeam.AddFollower.Request(kvp.Key, new FollowerInfo
                    {
                        Followers = info.Followers.ToList(),
                        UnderCommanders = info.UnderCommanders.ToList()
                    }));
                }
            }

            // SetSuperiors
            if (superiorDic != null) {
                foreach (var kvp in superiorDic)
                {
                    foreach (var f in kvp.Value)
                    {
                        this.CommandSystem.SendCommand(new CommanderTeam.SetSuperior.Request(f,
                                                        new SuperiorInfo { EntityId = kvp.Key }));
                    }
                }
            }

            // RegisterCommanderToHQ
            //if (commanders != null)
            //{
            //    foreach (var kvp in commanders)
            //    {
            //        this.CommandSystem.SendCommand(new CommandersManager.AddCommander.Request(kvp.Key,
            //                                        new CreatedCommanderList { Commanders = kvp.Value.ToList() }));
            //    }
            //}
        }

        //private void SetList(ref Dictionary<EntityId, List<CommanderInfo>> commanders, in EntityId product, uint rank)
        //{
        //    commanders = commanders ?? new Dictionary<EntityId, List<CommanderInfo>>();
        //
        //    List<CommanderInfo> list = null;
        //    commanders.TryGetValue(hqId, out list);
        //    list = list ?? new List<CommanderInfo>();
        //    list.Add(new CommanderInfo(product,rank));
        //    commanders[hqId] = list;
        //}

        private void SetList(ref Dictionary<EntityId, List<EntityId>> superiorDic, in EntityId superior, IEnumerable<EntityId> followers)
        {
            superiorDic = superiorDic ?? new Dictionary<EntityId, List<EntityId>>();

            List<EntityId> list = null;
            superiorDic.TryGetValue(superior, out list);
            list = list ?? new List<EntityId>();
            list.AddRange(followers);
            superiorDic[superior] = list;
        }
    }
}
