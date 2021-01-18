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
                ComponentType.ReadOnly<UnitFactory.HasAuthority>(),
                ComponentType.ReadWrite<ResourceComponent.Component>(),
                ComponentType.ReadOnly<ResourceComponent.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            factoryInter = IntervalCheckerInitializer.InitializedChecker(1.0f);

            checkerGroup = GetEntityQuery(
                ComponentType.ReadWrite<UnitFactory.Component>(),
                ComponentType.ReadOnly<UnitFactory.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>()
            );

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
            if (CheckTime(ref checkerInter) == false)
                return;

            Entities.With(checkerGroup).ForEach((ref UnitFactory.Component factory,
                                                 ref BaseUnitStatus.Component status,
                                                 ref Position.Component position) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsBuilding(status.Type))
                    return;

                var contaners = factory.Containers;
                int index = -1;
                var ids = new List<int>();
                foreach (var c in contaners) {
                    index++;

                    if (c.State != ContainerState.Created)
                        continue;

                    var list = getAllyUnits(status.Side, c.Pos.ToWorkerPosition(this.Origin), (float)RangeDictionary.TeamInter, allowDead:false, UnitType.Commander);
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
            if (CheckTime(ref factoryInter) == false)
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

                if (UnitUtils.IsBuilding(status.Type) == false)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                FollowerOrder? f_order = null;
                SuperiorOrder? s_order = null;
                TeamOrder? team_order = null;
                TurretOrder? turret_order = null;

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
                    team_order = factory.TeamOrders[0];
                    orderType = FactoryOrderType.Team;
                }
                else if (factory.TurretOrders.Count > 0) {
                    turret_order = factory.TurretOrders[0];
                    orderType = FactoryOrderType.Turret;
                }

                if (orderType == FactoryOrderType.None)
                    return;

                // calc time cost
                int resourceCost;
                float timeCost;
                if (CalcOrderCost(out resourceCost, out timeCost, f_order, s_order, team_order) == false)
                    return;

                Debug.LogFormat("ResourceCost:{0} TimeCost:{1}", resourceCost, timeCost);

                if (factory.CurrentType == FactoryOrderType.None) {
                    if (resource.Resource < resourceCost)
                    {
                        Debug.LogFormat("ResourcePoor:{0}", resource.Resource);
                        return;
                    }

                    factory.ProductInterval = IntervalCheckerInitializer.InitializedChecker(timeCost);
                    factory.CurrentType = orderType;
                    resource.Resource -= resourceCost;
                }

                factoryInter = factory.ProductInterval;
                if (CheckTime(ref factoryInter) == false)
                    return;

                Debug.LogFormat("CreateUnit!");

                factory.ProductInterval = factoryInter;

                var coords = GetEmptyCoordinates(entityId.EntityId, position.Coords, height_buffer, factory.Containers);
                EntityTemplate template = null;

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
                else if (team_order != null) {
                    CreateTeam(factory.TeamOrders, status.Side, entityId.EntityId, coords, out finished);
                }
                else if  (turret_order != null) {
                    // todo turret
                }

                if (finished)
                    factory.CurrentType = FactoryOrderType.None;
            });
        }

        readonly Dictionary<EntityId, VortexCoordsContainer> strDic = new Dictionary<EntityId, VortexCoordsContainer>();
        private Coordinates GetEmptyCoordinates(EntityId id, in Coordinates center, float height_buffer, List<UnitContainer> containers)
        {
            var index = containers.FindIndex(c => c.State == ContainerState.Empty);
            if (index >= 0) {
                containers.ChangeState(index, ContainerState.Reserved);
                return containers[index].Pos.ToCoordinates();
            }

            VortexCoordsContainer vortex = null;
            if (strDic.TryGetValue(id, out vortex) == false) {
                vortex = new VortexCoordsContainer(center, containers.Select(c => c.Pos.ToCoordinates()).ToList(), RangeDictionary.TeamInter, this.Origin, height_buffer);
                strDic[id] = vortex;
            }

            index = containers.Count;
            containers.Add(new UnitContainer() { Pos = vortex.AddPos().ToFixedPointVector3(), State = ContainerState.Reserved });
            return containers[index].Pos.ToCoordinates();
        }

        private Coordinates GetEmptyCoordinates(EntityId id, in Coordinates center, float heightBuffer, uint hexForward, Dictionary<uint,List<UnitContainer>> hexContainers)
        {
            if (hexContainers.ContainsKey(hexForward) == false)
                hexContainers[hexForward] = new List<UnitContainer>();

            var containers = hexContainers[hexForward];
            var index = containers.FindIndex(c => c.State == ContainerState.Empty);
            if (index >= 0) {
                containers.ChangeState(index, ContainerState.Reserved);
                return containers[index].Pos.ToCoordinates();
            }

            VortexCoordsContainer vortex = null;
            if (strDic.TryGetValue(id, out vortex) == false) {
                vortex = new VortexCoordsContainer(center, containers.Select(c => c.Pos.ToCoordinates()).ToList(), RangeDictionary.TeamInter, this.Origin, heightBuffer);
                strDic[id] = vortex;
            }

            index = containers.Count;
            containers.Add(new UnitContainer() { Pos = vortex.AddPos().ToFixedPointVector3(), State = ContainerState.Reserved });
            return containers[index].Pos.ToCoordinates();
        }

        private bool CalcOrderCost(out int resourceCost, out float timeCost,
                                    FollowerOrder? f_order = null, SuperiorOrder? s_order = null, TeamOrder? team_order = null)
        {
            resourceCost = 0;
            timeCost = 0;

            if (f_order != null && UnitFactoryDictionary.TryGetCost(f_order.Value.Type, out resourceCost, out timeCost))
                return true;

            if (s_order != null && UnitFactoryDictionary.TryGetCost(UnitType.Commander, out resourceCost, out timeCost))
                return true;

            if (team_order != null) {
                var number = team_order.Value.SoldiersNumber;
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
                    template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, current.Rank, superiorId, coords, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
                    break;
                default:
                    template = BaseUnitTemplate.CreateBaseUnitEntityTemplate(current.Side, current.Type, coords, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
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
            EntityTemplate template = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(current.Side, current.Rank, null, coords, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
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
            public int soldiersNumber;
            public HashSet<CommandRequestId> hashes;

            public bool IsReady
            {
                get
                {
                    if (soldiers == null)
                        return false;
                    
                    return soldiers.Count >= soldiersNumber && team.CommanderId.IsValid();
                }
            }
        }

        readonly Dictionary<EntityId,Dictionary<int,RequestInfo>> requestDic = new Dictionary<EntityId, Dictionary<int, RequestInfo>>();
        int currentRequestGroupId = 0;

        private class UnitOrderContext
        {
            public UnitType type;
            public int requestGroupId;
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
            var temp = BaseUnitTemplate.CreateCommanderUnitEntityTemplate(side, current.CommanderRank, null, coords, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
            templates.Add((temp, UnitType.Commander));

            var posList = GetCoordinates(coords, current.SoldiersNumber);
            foreach(var pos in posList) {
                temp = BaseUnitTemplate.CreateBaseUnitEntityTemplate(side, UnitType.Soldier, pos, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
                templates.Add((temp, UnitType.Soldier));
            }

            var hashes = new HashSet<CommandRequestId>();

            foreach(var pair in templates) {
                var comId = this.CommandSystem.SendCommand(new WorldCommands.CreateEntity.Request(
                    pair.Item1,
                    context: new UnitOrderContext() { type = pair.Item2, 
                                                      requestGroupId = currentRequestGroupId,
                                                      strongholdEntityId = id }
                ));

                hashes.Add(comId);
            }

            Dictionary<int, RequestInfo> dic = null;
            if (requestDic.TryGetValue(id, out dic) == false)
                dic = new Dictionary<int, RequestInfo>();

            dic.Add(currentRequestGroupId, new RequestInfo()
            {
                team = new TeamInfo() { CommanderId = new EntityId(),
                                        Rank = current.CommanderRank,
                                        Order = current.Order,
                                        TargetInfoSet = TargetUtils.DefaultTargteInfoSet(),
                                        StrongholdEntityId = id },
                soldiers = new List<EntityId>(),
                soldiersNumber = current.SoldiersNumber,
                hashes = hashes
            });

            requestDic[id] = dic;

            currentRequestGroupId++;

            if (current.Stack <= 1)
                orders.RemoveAt(0);
            else {
                current.Stack--;
                orders[0] = current;
            }

            finished = true;
        }

        void CreateTurret(List<TurretOrder> orders, UnitSide side, EntityId id, in Coordinates coords, out bool finished)
        {
            finished = false;
            if (orders.Count == 0)
                return;

            var current = orders[0];
            // create unit
            List<ValueTuple<EntityTemplate,UnitType>> templates = new List<ValueTuple<EntityTemplate,UnitType>>();

            var posList = GetCoordinates(coords, current.TurretsNumber);
            foreach(var pos in posList) {
                var temp = BaseUnitTemplate.CreateTurretUnitTemplate(side, current.TurretId, pos, TransformUtils.ToAngleAxis(current.Rot, Vector3.up));
                templates.Add((temp, UnitType.Turret));
            }

            foreach(var pair in templates) {
                this.CommandSystem.SendCommand(new WorldCommands.CreateEntity.Request(
                    pair.Item1,
                    context: new UnitOrderContext() { type = pair.Item2, 
                                                      requestGroupId = currentRequestGroupId,
                                                      strongholdEntityId = id }
                ));
            }

            //Dictionary<int, RequestInfo> dic = null;
            //if (requestDic.TryGetValue(id, out dic) == false)
            //    dic = new Dictionary<int, RequestInfo>();
            //
            //dic.Add(currentRequestId, new RequestInfo()
            //{
            //    team = new TeamInfo() { CommanderId = new EntityId(),
            //                            Rank = current.CommanderRank,
            //                            Order = current.Order,
            //                            TargetEntityId = new EntityId(),
            //                            StrongholdEntityId = id },
            //    soldiers = new List<EntityId>(),
            //    stack = current.Stack
            //});
            //
            //requestDic[id] = dic;
            //
            currentRequestGroupId++;
            //
            //if (current.Stack <= 1)
            //    orders.RemoveAt(0);
            //else {
            //    current.Stack--;
            //    orders[0] = current;
            //}

            finished = true;
        }

        readonly Dictionary<Coordinates,VortexCoordsContainer> vortexDic = new Dictionary<Coordinates, VortexCoordsContainer>();

        List<Coordinates> GetCoordinates(in Coordinates coords, int num)
        {
            VortexCoordsContainer container = null;
            if (vortexDic.TryGetValue(coords, out container) == false) {
                container = new VortexCoordsContainer(coords, RangeDictionary.UnitInter, this.Origin, height_buffer);
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

                if (response.Context is UnitOrderContext unitOrderContext) {
                    HandleUnitOrderContext(unitOrderContext, response.RequestId, response.EntityId.Value);
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

        private void HandleUnitOrderContext(UnitOrderContext unitOrderContext, CommandRequestId comId, EntityId entityId)
        {
            var strongholdId = unitOrderContext.strongholdEntityId;
            if (requestDic.ContainsKey(strongholdId) == false)
                return;

            var dic = requestDic[strongholdId];
            var requestId = unitOrderContext.requestGroupId;
            if (dic.ContainsKey(requestId) == false)
                return;

            var requestInfo = dic[requestId];
            if (requestInfo.hashes.Contains(comId) == false)
                return;

            requestInfo.hashes.Remove(comId);
            var type = unitOrderContext.type;
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

                var solds = string.Empty;
                foreach (var s in requestInfo.soldiers)
                    solds += string.Format("{0}, ", s);

                Debug.LogFormat("SetTeam CommanderId:{0} Soldiers:{1}", requestInfo.team.CommanderId, solds);
            }
            else {
                requestDic[strongholdId][requestId] = requestInfo;
            }
        }
    }

    public abstract class CoordsContainer
    {
        protected readonly List<Coordinates> posList = new List<Coordinates>();
        protected Coordinates center = Coordinates.Zero;
        protected double inter = 0;
        protected Vector3 origin;
        protected float height_buffer;

        public CoordsContainer(in Coordinates center, double inter, Vector3 origin, float height_buffer)
        {
            this.inter = inter;
            Reset(center);
            this.origin = origin;
            this.height_buffer = height_buffer;
        }

        public CoordsContainer(in Coordinates center, List<Coordinates> posList, double inter, Vector3 origin, float height_buffer)
        {
            this.center = center.GetGrounded(origin, height_buffer);
            this.posList = posList;
            this.inter = inter;
            this.origin = origin;
            this.height_buffer = height_buffer;
        }

        protected virtual void Reset(in Coordinates center)
        {
            this.center = center.GetGrounded(origin, height_buffer);
            posList.Clear();
        }

        public List<Coordinates> GetCoordinates(in Coordinates coords, int num)
        {
            if (center != coords) 
                Reset(coords);

            int count = posList.Count;
            if (num <= count)
                return posList.Take(num).ToList();

            foreach(var i in Enumerable.Range(0,num - count)) {
                AddPos();
            }

            return posList;
        }

        public abstract Coordinates AddPos();
    }

    public class VortexCoordsContainer
    {
        readonly List<Coordinates> posList = new List<Coordinates>();
        Coordinates center = Coordinates.Zero;
        int edge = 1;
        int index = 0;
        double inter = 0;
        Vector3 origin;
        float height_buffer;

        public VortexCoordsContainer(in Coordinates center, double inter, Vector3 origin, float height_buffer)
        {
            this.inter = inter;
            Reset(center);
            this.origin = origin;
            this.height_buffer = height_buffer;
        }

        public VortexCoordsContainer(in Coordinates center, List<Coordinates> posList, double inter, Vector3 origin, float height_buffer)
        {
            this.center = center.GetGrounded(origin, height_buffer);
            this.posList = posList;
            this.inter = inter;
            this.origin = origin;
            this.height_buffer = height_buffer;

            index = posList.Count;
            edge = 1;
            while (index > edge * 8) {
                index -= edge * 8;
                edge++;
            }
        }

        private void Reset(in Coordinates center)
        {
            this.center = center.GetGrounded(origin, height_buffer);
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

            foreach(var i in Enumerable.Range(0,num - count)) {
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
            pos = pos.GetGrounded(this.origin, this.height_buffer);
            posList.Add(pos);
            index++;

            return pos;
        }
    }

    public class TriangleCoordsContainer : CoordsContainer
    {
        int index = 0;

        public TriangleCoordsContainer(in Coordinates center, double inter, Vector3 origin, float height_buffer) : base(center, inter, origin, height_buffer)
        {
        }

        public TriangleCoordsContainer(in Coordinates center, List<Coordinates> posList, double inter, Vector3 origin, float height_buffer) : base(center, posList, inter, origin, height_buffer)
        {
            index = posList.Count;
        }

        protected override void Reset(in Coordinates center)
        {
            base.Reset(center);
            index = 0;
        }

        public override Coordinates AddPos()
        {
            var count = posList.Count;

            Coordinates pos;
 
            int layerNum = 1;
            double x = inter;
            double z = 0;
            while (count >= layerNum) {
                count -= layerNum;
                layerNum += 2;

                x += inter;
                z -= inter;
            }

            z += count * inter;
            pos = this.center + new Coordinates(x, 0, z);
            pos = pos.GetGrounded(this.origin, this.height_buffer);
            posList.Add(pos);
            index++;

            return pos;
        }
    }

    public class RandomContainer
    {
        Vector3 corner;
        Vector3 size;
        int cut_x;
        int cut_z;

        readonly bool[,] vertexes;

        public RandomContainer(Vector3 center, Vector3 size, int cut_x, int cut_z)
        {
            this.corner = center - size/2;
            this.size = size;
            this.cut_x = cut_x;
            this.cut_z = cut_z;

            vertexes = new bool[cut_x,cut_z];
        }

        private bool TryGetPoint(Vector3 point, out int x, out int z)
        {
            x = -1;
            z = -1;
            var diff = point - this.corner;
            if (diff.x < 0 || diff.x > this.size.x)
                return false;

            if (diff.z < 0 || diff.z > this.size.z)
                return false;

            x = (int)(diff.x * cut_x / size.x); 
            z = (int)(diff.z * cut_z / size.z);

            return true;
        }

        public bool AddPoint(Vector3 point)
        {
            if (TryGetPoint(point, out var x, out var z) == false)
                return false;

            if (vertexes[x,z] == false) {
                vertexes[x,z] = true;
                return true;
            }
            else
                return false;
        }

        public Vector3? GetEmptyPoint()
        {
            for (var i = 0; i < cut_x; i++) {
                for (var j = 0; j < cut_z; j++) {
                    if (vertexes[i,j])
                        continue;

                    vertexes[i,j] = true;
                    return new Vector3((2*i+1)*size.x/2/cut_x, (2*j+1)*size.z/2/cut_z);
                }
            }

            return null;
        }

        public void SetCurrentPoints(Vector3[] points)
        {
            Clear();
            foreach (var p in points)
            {
                AddPoint(p);
            }
        }

        public void Clear()
        {
            for (var i = 0; i < cut_x; i++) {
                for (var j = 0; j < cut_z; j++)
                    vertexes[i,j] = false;
            }
        }
    }

    public class HexRandomContaner
    {
        readonly Dictionary<int,RandomContainer> containers = new Dictionary<int,RandomContainer>();
        Vector3 center;
        float radius;
        int cut;

        public HexRandomContaner(Vector3 center, float radius, int cut)
        {
            this.center = center;
            this.radius = radius;
            this.cut = cut;
        }

        const float degTri = 60.0f;
        const float root3 = 1.7320f;
        public Vector3? GetRandomPoint(Vector3 point)
        {
            var diff = point - this.center;
            var length = diff.x * diff.x + diff.z * diff.z;
            if (length > radius * radius)
                return null;

            var nor = diff.normalized;
            var deg = Mathf.Atan2(nor.x, nor.z) * Mathf.Rad2Deg;
            int index = (int)(deg / degTri);
            if (deg < 0)
                index --;

            if (index > 2 || index < -3)
                return null;

            if (containers.ContainsKey(index) == false) {
                var rad = degTri/2*(2*index+1);
                var range = radius / root3;
                var pos = center + range * new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad));

                containers[index] = new RandomContainer(pos, range/2 * Vector3.one, cut, cut);
            }

            return containers[index].GetEmptyPoint();
        }

        public void SetCurrentPoints(Vector3[] points)
        {
            foreach (var kvp in containers)
                kvp.Value.SetCurrentPoints(points);
        }
    }
}
