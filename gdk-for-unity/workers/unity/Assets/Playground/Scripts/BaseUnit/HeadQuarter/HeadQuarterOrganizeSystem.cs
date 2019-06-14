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

                    var id = tgt.TargetInfo.TargetId;
                    var request = new UnitFactory.AddOrder.Request(id, new ProductOrder() { Customer = order.Customer,
                                                                                             Number = 1,
                                                                                             Type = UnitType.Commander,
                                                                                             Side = status.Side,
                                                                                             CommanderRank = order.CustomerRank + 1 });
                    Entity entity;
                    if (TryGetEntity(id, out entity))
                    {
                        commandSystem.SendCommand(request, entity);
                        // add
                    }
                }

                hqData[i] = headQuarter;
            }
        }
    }
}
