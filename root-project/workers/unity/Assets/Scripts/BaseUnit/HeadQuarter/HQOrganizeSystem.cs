using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class HQOrganizeSystem : BaseSearchSystem
    {
        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<HeadQuarters.Component>(),
                ComponentType.ReadOnly<HeadQuarters.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(HeadQuarters.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref HeadQuarters.Component headQuarter,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.HeadQuarter)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (headQuarter.Orders.Count == 0)
                    return;

                if (headQuarter.UpperRank >= headQuarter.MaxRank)
                    return;

                var inter = headQuarter.Interval;
                if (inter.CheckTime() == false)
                    return;

                headQuarter.Interval = inter;

                foreach (var order in headQuarter.Orders) {
                    var pos = order.Pos.ToWorkerPosition(this.Origin);
                    var str = getNearestAlly(status.Side, pos, RangeDictionary.Get(FixedRangeType.RadioRange), UnitType.Stronghold);
                    if (str == null)
                        continue;

                    var map = headQuarter.FactoryDatas;
                    uint u_rank;
                    SetSuperior(str.id, status.Side, order, ref map, entityId.EntityId, out u_rank);
                    headQuarter.FactoryDatas = map;
                    if (headQuarter.UpperRank < u_rank)
                        headQuarter.UpperRank = u_rank;
                }

                headQuarter.Orders.Clear();
            });
        }

        void SetSuperior(EntityId id, UnitSide side, in OrganizeOrder order, ref FactoryMap map, in EntityId entityId, out uint upper_rank)
        {
            upper_rank = 0;

            if (map.Reserves.ContainsKey(id) == false)
                map.Reserves.Add(id, new ReserveMap() { Datas = new Dictionary<uint, ReserveInfo>() });
            var reserve = map.Reserves[id];

            var rank = order.CustomerRank;
            if (reserve.Datas.ContainsKey(rank) == false)
                reserve.Datas.Add(rank, new ReserveInfo { Followers = new List<EntityId>() });
            var info = reserve.Datas[rank];

            info.Followers.Add(order.Customer);
            if (info.Followers.Count < 1)
            {
                map.Reserves[id] = reserve;
                return;
            }

            upper_rank = rank + 1;
            var request = new UnitFactory.AddSuperiorOrder.Request(id, new SuperiorOrder() { Followers = info.Followers.ToList(),
                                                                                             HqEntityId = entityId,
                                                                                             Side = side,
                                                                                             Rank = rank + 1 });
            Entity entity;
            if (TryGetEntity(id, out entity))
                this.CommandSystem.SendCommand(request, entity);

            info.Followers.Clear();
            reserve.Datas[rank] = info;
            map.Reserves[id] = reserve;
        }
    }
}
