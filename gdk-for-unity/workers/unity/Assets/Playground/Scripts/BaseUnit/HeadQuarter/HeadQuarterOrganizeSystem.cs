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
    [UpdateBefore(typeof(FixedUpdate.PhysicsFixedUpdate))]
    internal class HeadQuarterOrganizeSystem : BaseSearchSystem
    {
        private CommandSystem commandSystem;
        private ComponentGroup group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            commandSystem = World.GetExistingManager<CommandSystem>();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;

            group = GetComponentGroup(
                ComponentType.Create<HeadQuarters.Component>(),
                ComponentType.ReadOnly<HeadQuarters.ComponentAuthority>(),
                ComponentType.ReadOnly<CommanderStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(HeadQuarters.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var hqData = group.GetComponentDataArray<HeadQuarters.Component>();
            var commanderData = group.GetComponentDataArray<CommanderStatus.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var tgtData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < hqData.Length; i++)
            {
                var headQuarter = hqData[i];
                var commander = commanderData[i];
                var status = statusData[i];
                var tgt = tgtData[i];
                var trans = transData[i];
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type == UnitType.HeadQuarter)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                if (headQuarter.Orders.Count == 0)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = headQuarter.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                headQuarter.Interval = inter;

                const float range = 500.0f;
                foreach (var order in  headQuarter.Orders)
                {
                    var pos = order.Pos.ToUnityVector();
                    var str = getNearestAlly(status.Side, pos, range, UnitType.Stronghold);
                    if (str == null)
                        continue;

                    SetSuperior(tgt.TargetInfo.TargetId, status.Side, order, ref headQuarter.FactoryDatas);
                }

                headQuarter.Orders.Clear();
                hqData[i] = headQuarter;
            }
        }

        void SetSuperior(EntityId id, UnitSide side, in OrganizeOrder order, ref FactoryMap map)
        {
            if (map.Reserve.ContaisKey(id) == false)
                map.Reserve.Add(id, new ReserveMap());
            var reserve = map.Reserve[id];

            var rank = order.CustomerRank;
            if (reserve.Datas.ContainsKey(rank) == false)
                reserve.Datas.Add(rank, new List<EntityId>());
            var list = reserve.Datas[rank];

            list.Add(order.Customer);
            if (list.Count < 3)
            {
                map.Reserve[id] = reserve;
                return;
            }

            var request = new UnitFactory.AddSuperiorOrder.Request(id, new SuperiorOrder() { Followers = list.ToList(),
                                                                                             Side = side,
                                                                                             Rank = rank + 1 });
            Entity entity;
            if (TryGetEntity(id, out entity))
                commandSystem.SendCommand(request, entity);

            list.Clear();
            map.Reserve[id] = reserve;
        }
    }
}
