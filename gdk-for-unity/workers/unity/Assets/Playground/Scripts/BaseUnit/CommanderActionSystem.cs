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
    internal class CommanderActionSystem : BaseSearchSystem
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
                ComponentType.Create<CommanderAction.Component>(),
                ComponentType.ReadOnly<CommanderAction.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(CommanderAction.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var actionData = group.GetComponentDataArray<CommanderAction.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var tgtData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < actionData.Length; i++)
            {
                var action = actionData[i];
                var status = statusData[i];
                var tgt = tgtData[i];
                var trans = transData[i];
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type == UnitType.Commander)
                    continue;

                if (!action.IsTarget)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = action.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                action.Interval = inter;

                if (status.Order == OrderType.Escape && action.Type != CommandActionType.Product)
                {
                    var diff = tgt.TargetInfo.Position.ToUnityVector() - trans.position;

                    float length = 10.0f;   // TODO from:master
                    int num = 5;
                    if (diff.sqrMagnitude < diff.sqrMagnitude)
                    {
                        var id = tgt.TargetInfo.TargetId;
                        var request = new UnitFactory.SendOrder.Request(id, new ProductOrder() { Customer = entityId.EntityId,
                                                                                                 Number = num,
                                                                                                 Type = UnitType.Soldier,
                                                                                                 Side = status.Side });

                        Entity entity;
                        if (TryGetEntity(id, out entity))
                        {
                            commandSystem.SendCommand(request, entity);
                            action.Type = CommandActionType.Product;
                        }
                    }
                }

                actionData[i] = action;
            }
        }
    }
}
