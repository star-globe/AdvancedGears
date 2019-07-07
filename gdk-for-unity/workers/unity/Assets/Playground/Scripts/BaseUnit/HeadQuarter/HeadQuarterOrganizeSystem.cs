using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class HeadQuarterOrganizeSystem : BaseSearchSystem
    {
        private CommandSystem commandSystem;
        private EntityQuery group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            commandSystem = World.GetExistingSystem<CommandSystem>();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<HeadQuarters.Component>(),
                ComponentType.ReadOnly<HeadQuarters.ComponentAuthority>(),
                ComponentType.ReadOnly<CommanderStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(HeadQuarters.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref HeadQuarters.Component headQuarter,
                                          ref CommanderStatus.Component commander,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type == UnitType.HeadQuarter)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (headQuarter.Orders.Count == 0)
                    return;

                // TODO:upper check 
                if (headQuarter.UpperRank >= 5)
                    return;

                var time = Time.realtimeSinceStartup;
                var inter = headQuarter.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                headQuarter.Interval = inter;

                const float range = 500.0f;
                foreach (var order in headQuarter.Orders)
                {
                    var pos = order.Pos.ToUnityVector();
                    var str = getNearestAlly(status.Side, pos, range, UnitType.Stronghold);
                    if (str == null)
                        continue;

                    var map = headQuarter.FactoryDatas;
                    uint u_rank;
                    SetSuperior(tgt.TargetInfo.TargetId, status.Side, order, ref map, out u_rank);
                    headQuarter.FactoryDatas = map;
                    if (headQuarter.UpperRank < u_rank)
                        headQuarter.UpperRank = u_rank;
                }

                headQuarter.Orders.Clear();
            });
        }

        void SetSuperior(EntityId id, UnitSide side, in OrganizeOrder order, ref FactoryMap map, out uint upper_rank)
        {
            upper_rank = 0;

            if (map.Reserves.ContainsKey(id) == false)
                map.Reserves.Add(id, new ReserveMap());
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
                                                                                             Side = side,
                                                                                             Rank = rank + 1 });
            Entity entity;
            if (TryGetEntity(id, out entity))
                commandSystem.SendCommand(request, entity);

            info.Followers.Clear();
            reserve.Datas[rank] = info;
            map.Reserves[id] = reserve;
        }
    }
}
