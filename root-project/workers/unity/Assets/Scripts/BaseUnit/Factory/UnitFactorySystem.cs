using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.TransformSynchronization;
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
    public class UnitFactorySystem : BaseSearchSystem
    {
        EntityQuery factoryGroup;
        IntervalChecker factoryInter;

        EntityQuery checkerGroup;
        IntervalChecker checkerInter;

        private class ProductOrderContext
        {
            public FollowerOrder? f_order;
            public SuperiorOrder? s_order;
            public UnitType type;
            public EntityId strongholdId;
            public UnitContainer container;
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

            factoryGroup = GetEntityQuery(
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.ComponentAuthority>(),
                ComponentType.ReadWrite<ResourceComponent.Component>(),
                ComponentType.ReadOnly<ResourceComponent.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            factoryGroup.SetFilter(UnitFactory.ComponentAuthority.Authoritative);
            factoryGroup.SetFilter(ResourceComponent.ComponentAuthority.Authoritative);

            factoryInter = IntervalCheckerInitializer.InitializedChecker(1.0f);

            checkerGroup = GetEntityQuery(
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>()
            );
            checkerGroup.SetFilter(UnitFactory.ComponentAuthority.Authoritative);

            checkerInter = IntervalCheckerInitializer.InitializedChecker(1.5f);
        }

        protected override void OnUpdate()
        {
            HandleUnitCheck();
            HandleProductUnit();
            HandleProductResponse();
        }

        private void HandleUnitCheck()
        {
            if (checkerInter.CheckTime() == false)
                return;

            Entities.With(checkerGroup).ForEach((ref UnitFactory.Component factory,
                                                 ref BaseUnitStatus.Component status,
                                                 ref Position.Component position) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var contaners = factory.Containers;
                int index = -1;
                var ids = new List<int>();
                foreach (var c in contaners) {
                    index++;

                    if (c.State != ContainerState.Created)
                        continue;

                    var list = getAllyUnits(status.Side, c.Pos.ToWorkerPosition(this.Origin), (float)RangeDictionary.TeamInter, UnitType.Commander);
                    if (list.Count == 0)
                        ids.Add(index);
                }

                if (ids.Count == 0)
                    return;

                foreach (var i in ids)
                    contaners.ChangeState(i,ContainerState.Empty);

                factory.Containers = contaners;
            });
        }

        // TODO:getFromSettings;
        const float range = 12.0f;
        const float height_buffer = 5.0f;
        void HandleProductUnit()
        {
            if (factoryInter.CheckTime() == false)
                return;

            Entities.With(factoryGroup).ForEach((Unity.Entities.Entity entity,
                                          ref UnitFactory.Component factory,
                                          ref ResourceComponent.Component resource,
                                          ref BaseUnitStatus.Component status,
                                          ref Position.Component position,
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

                if (orderType == FactoryOrderType.None)
                    return;

                // calc time cost
                int resourceCost;
                float timeCost;
                if (CalcOrderCost(out resourceCost, out timeCost, f_order, s_order, t_order) == false)
                    return;

                Debug.LogFormat("ResourceCost:{0} TimeCost:{1}", resourceCost, timeCost);

                if (factory.CurrentType == FactoryOrderType.None) {
                    if (resource.Resource < resourceCost)
                    {
                        Debug.LogFormat("ResourcePoor:{0}", resource.Resource);
                        return;
                    }

                    factory.ProductInterval = IntervalCheckerInitializer.InitializedChecker(timeCost);//new IntervalChecker(timeCost, time + timeCost, 0, -1);   // TODO modify
                    factory.CurrentType = orderType;
                    resource.Resource -= resourceCost;
                }

                factoryInter = factory.ProductInterval;
                if (factoryInter.CheckTime() == false)
                    return;

                Debug.LogFormat("CreateUnit!");

                factory.ProductInterval = factoryInter;

                //var trans = EntityManager.GetComponentObject<Transform>(entity);
                //var p = trans.position + RandomLogic.XZRandomCirclePos(range);
                var pos = GetEmptyCoordinates(entityId.EntityId, position.Coords, factory.Containers);
                //var pos = p - this.Origin;
                EntityTemplate template = null;
                var coords = new Coordinates(pos.X, pos.Y + height_buffer, pos.Z);

                bool finished = false;
                UnitType type = UnitType.None;
                if (s_order != null)
                {
                    template = CreateSuperior(factory.SuperiorOrders, coords, out finished);
                    type = UnitType.Commander;
                }
                else if (f_order != null)
                {
                    template = CreateFollower(factory.FollowerOrders, coords, f_order.Value.Customer, out finished);
                    type = UnitType.Soldier;
                }

                if (template != null) {
                    var request = new WorldCommands.CreateEntity.Request
                    (
                        template,
                        context: new ProductOrderContext() { f_order = f_order,
                                                             s_order = s_order,
                                                             type = type,
                                                             strongholdId = entityId.EntityId,
                                                             container = new UnitContainer(coords.ToFixedPointVector3(), ContainerState.Created) }
                    );
                    this.CommandSystem.SendCommand(request);
                }
                else if (t_order != null) {
                    CreateTeam(factory.TeamOrders, status.Side, entityId.EntityId, coords, out finished);
                }

                if (finished)
                    factory.CurrentType = FactoryOrderType.None;
            });
        }

        readonly Dictionary<EntityId, VortexCoordsContainer> strDic = new Dictionary<EntityId, VortexCoordsContainer>();
        private Coordinates GetEmptyCoordinates(EntityId id, in Coordinates center, List<UnitContainer> containers)
        {
            var index = containers.FindIndex(c => c.State == ContainerState.Empty);
            if (index >= 0) {
                containers.ChangeState(index, ContainerState.Reserved);
                return containers[index].Pos.ToCoordinates();
            }

            VortexCoordsContainer vortex = null;
            if (strDic.TryGetValue(id, out vortex) == false) {
                vortex = new VortexCoordsContainer(center, containers.Select(c => c.Pos.ToCoordinates()).ToList(), RangeDictionary.TeamInter);
                strDic[id] = vortex;
            }

            index = containers.Count;
            containers.Add(new UnitContainer() { Pos = vortex.AddPos().ToFixedPointVector3(), State = ContainerState.Reserved });
            return containers[index].Pos.ToCoordinates();
        }

        private bool CalcOrderCost(out int resourceCost, out float timeCost,
                                    FollowerOrder? f_order = null, SuperiorOrder? s_order = null, TeamOrder? t_order = null)
        {
            resourceCost = 0;
            timeCost = 0;

            if (f_order != null && UnitFactoryDictionary.TryGetCost(f_order.Value.Type, out resourceCost, out timeCost))
                return true;

            if (s_order != null && UnitFactoryDictionary.TryGetCost(UnitType.Commander, out resourceCost, out timeCost))
                return true;

            if (t_order != null) {
                var number = t_order.Value.SoldiersNumber;
                int solResource;
                float solTime;
                if (UnitFactoryDictionary.TryGetCost(UnitType.Commander, out resourceCost, out timeCost) &&
                    UnitFactoryDictionary.TryGetCost(UnitType.Soldier, out solResource, out solTime)) {
                    resourceCost += solResource * number;
                    timeCost += solTime * number;
                    return true;
                }
            }

            return false;
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
            public int stack;

            public bool IsReady
            {
                get
                {
                    if (soldiers == null)
                        return false;
                    
                    return soldiers.Count > stack && team.CommanderId.IsValid();
                }
            }
        }

        readonly Dictionary<EntityId,Dictionary<int,RequestInfo>> requestDic = new Dictionary<EntityId, Dictionary<int, RequestInfo>>();
        int currentRequestId = 0;

        private class TeamOrderContext
        {
            public UnitType type;
            public int requestId; 
            public EntityId strongholdEntityId;
        }

        void CreateTeam(List<TeamOrder> orders, UnitSide side, EntityId id, in Coordinates coords, out bool finished)
        {
            finished = false;
            if (orders.Count == 0)
                return;

            var current = orders[0];
            // create unit
            List<ValueTuple<EntityTemplate,UnitType>> templates = new List<ValueTuple<EntityTemplate,UnitType>>();
            var temp = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(side, coords, current.CommanderRank, null);
            templates.Add((temp, UnitType.Commander));

            var posList = GetCoordinates(coords, current.SoldiersNumber);
            foreach(var pos in posList) {
                temp = BaseUnitTemplate.CreateBaseUnitEntityTemplate(side, pos, UnitType.Soldier);
                templates.Add((temp, UnitType.Soldier));
            }

            foreach(var pair in templates) {
                this.CommandSystem.SendCommand(new WorldCommands.CreateEntity.Request(
                    pair.Item1,
                    context: new TeamOrderContext() { type = pair.Item2, 
                                                      requestId = currentRequestId,
                                                      strongholdEntityId = id }
                ));
            }

            Dictionary<int, RequestInfo> dic = null;
            if (requestDic.TryGetValue(id, out dic) == false)
                dic = new Dictionary<int, RequestInfo>();

            dic.Add(currentRequestId, new RequestInfo()
            {
                team = new TeamInfo() { CommanderId = new EntityId(),
                                        Rank = current.CommanderRank,
                                        Order = current.Order,
                                        TargetEntityId = new EntityId(),
                                        StrongholdEntityId = id },
                soldiers = new List<EntityId>(),
                stack = current.Stack
            });

            requestDic[id] = dic;

            currentRequestId++;

            if (current.Stack <= 1)
                orders.RemoveAt(0);
            else {
                current.Stack--;
                orders[0] = current;
            }

            finished = true;
        }

        readonly Dictionary<Coordinates,VortexCoordsContainer> vortexDic = new Dictionary<Coordinates, VortexCoordsContainer>();

        List<Coordinates> GetCoordinates(in Coordinates coords, int num)
        {
            VortexCoordsContainer container = null;
            if (vortexDic.TryGetValue(coords, out container) == false) {
                container = new VortexCoordsContainer(coords, RangeDictionary.UnitInter);
                vortexDic.Add(coords, container);
            }

            return container.GetCoordinates(coords, num);
        }

        void HandleProductResponse()
        {
            Dictionary<EntityId, FollowerInfo> followerDic = null;
            Dictionary<EntityId, List<EntityId>> superiorDic = null;
            Dictionary<EntityId, List<CommanderInfo>> commanders = null;

            var responses = this.CommandSystem.GetResponses<WorldCommands.CreateEntity.ReceivedResponse>();
            for (var i = 0; i < responses.Count; i++) {
                ref readonly var response = ref responses[i];
                if (response.StatusCode != StatusCode.Success)
                    continue;

                if (response.Context is ProductOrderContext requestContext) {
                    HandleProductOrderContext(followerDic, superiorDic, commanders, requestContext, response.EntityId.Value);
                    continue;
                }

                if (response.Context is TeamOrderContext teamOrderContext) {
                    HandleTeamOrderContext(teamOrderContext, response.EntityId.Value);
                    continue;
                }
            }
        }

        private void HandleProductOrderContext(Dictionary<EntityId, FollowerInfo> followerDic,
                                               Dictionary<EntityId, List<EntityId>> superiorDic,
                                               Dictionary<EntityId, List<CommanderInfo>> commanders,
                                               ProductOrderContext requestContext,
                                               EntityId entityId)
        {
            var order = requestContext;
            if (order.f_order != null) {
                followerDic = followerDic ?? new Dictionary<EntityId, FollowerInfo>();
                var id = order.f_order.Value.Customer;
                if (followerDic.ContainsKey(id) == false)
                    followerDic.Add(id, new FollowerInfo { Followers = new List<EntityId>(),
                                                           UnderCommanders = new List<EntityId>() });
                var info = followerDic[id];
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
                SetList(ref superiorDic, entityId, order.s_order.Value.Followers);
                //SetList(ref commanders, id, order.CommanderRank);
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
            this.CommandSystem.SendCommand(new UnitFactory.SetContainer.Request(order.strongholdId, order.container));
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

        private void HandleTeamOrderContext(TeamOrderContext teamOrderContext, EntityId entityId)
        {
            var strongholdId = teamOrderContext.strongholdEntityId;
            if (requestDic.ContainsKey(strongholdId) == false)
                return;

            var dic = requestDic[strongholdId];
            var requestId = teamOrderContext.requestId;
            if (dic.ContainsKey(requestId) == false)
                return;

            var requestInfo = dic[requestId];
            var type = teamOrderContext.type;
            switch (type)
            {
                case UnitType.Soldier:
                    requestInfo.soldiers.Add(entityId); 
                    break;

                case UnitType.Commander:
                    requestInfo.team.CommanderId = entityId;
                    break;

                default:
                    return;
            }

            if (requestInfo.IsReady) {
                this.CommandSystem.SendCommand(new CommanderTeam.AddFollower.Request(requestInfo.team.CommanderId,
                                                                                     new FollowerInfo() { Followers = requestInfo.soldiers,
                                                                                                          UnderCommanders = new List<EntityId>()}));
                requestDic[strongholdId].Remove(requestId);
            }
            else {
                requestDic[strongholdId][requestId] = requestInfo;
            }
        }
    }

    public class VortexCoordsContainer
    {
        readonly List<Coordinates> posList = new List<Coordinates>();
        Coordinates center = Coordinates.Zero;
        int edge = 1;
        int index = 0;
        double inter = 0;

        public VortexCoordsContainer(in Coordinates coords, double inter)
        {
            this.inter = inter;
            Reset(coords);
        }

        public VortexCoordsContainer(in Coordinates center, List<Coordinates> posList, double inter)
        {
            this.center = center;
            this.posList = posList;
            this.inter = inter;

            index = posList.Count;
            edge = 1;
            while (index > edge * 8) {
                index -= edge * 8;
                edge++;
            }
        }

        private void Reset(in Coordinates coords)
        {
            this.center = coords;
            edge = 1;
            index = 0;
            posList.Clear();
        }

        public List<Coordinates> GetCoordinates(in Coordinates coords, int num)
        {
            if (center != coords) 
                Reset(coords);

            int count = posList.Count;
            if (num <= count)
                return posList.Take(num).ToList();

            foreach(var i in Enumerable.Range(0,count - num)) {
                AddPos();
            }

            return posList;
        }

        public Coordinates AddPos()
        {
            var first = posList.Count;

            Coordinates pos;
            if (first == 0)
                pos = this.center;
            else
                pos = posList[first - 1];

            if (index == 8 * edge + 1) {
                edge++;
                index = 0;
            }

            double x = 0, z = 0;
            if (index == 0) {
                x = inter;
                z = inter;
            }
            else if (index <= 2 * edge)
                z = -inter;
            else if (index <= 4 * edge)
                x = -inter;
            else if (index <= 6 * edge)
                z = inter;
            else if (index <= 8 * edge)
                x = inter;

            pos += new Coordinates(x, 0, z);
            posList.Add(pos);
            index++;

            return pos;
        }
    }

}
