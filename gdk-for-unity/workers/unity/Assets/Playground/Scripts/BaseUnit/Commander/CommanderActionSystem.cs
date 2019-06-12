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
                ComponentType.ReadOnly<CommanderStatus.Component>(),
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
            var commanderData = group.GetComponentDataArray<CommanderStatus.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var tgtData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < actionData.Length; i++)
            {
                var action = actionData[i];
                var commander = commanderData[i];
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

                switch (status.Order)
                {
                    case OrderType.Escape:
                        ProductAlly(trans.position, status.Side, entityId, tgt, ref action);
                        break;

                    case OrderType.Organize:
                        OrganizeAlly(trans.position, status.Side, commander.Rank, entityId, tgt, ref action);
                        break;

                    default:
                        action.ActionType = CommandActionType.None;
                        break;
                }

                actionData[i] = action;
            }
        }

        void ProductAlly(in Vector3 pos, UnitSide side, in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetInfo.Position.ToUnityVector() - pos;
            float length = 10.0f;   // TODO from:master
            int num = 5;
            if (diff.sqrMagnitude < diff.sqrMagnitude)
            {
                var id = tgt.TargetInfo.TargetId;
                var request = new UnitFactory.AddOrder.Request(id, new ProductOrder() { Customer = entityId.EntityId,
                                                                                         Number = num,
                                                                                         Type = UnitType.Soldier,
                                                                                         Side = side });
                Entity entity;
                if (TryGetEntity(id, out entity))
                {
                    commandSystem.SendCommand(request, entity);
                    action.ActionType = CommandActionType.Product;
                }
            }
        }

        void OrganizeAlly(in Vector3 pos, UnitSide side, unit32 rank, in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Organize)
                return;

            var diff = tgt.TargetInfo.Position.ToUnityVector() - pos;
            float length = 10.0f;   // TODO from:master
            int num = 5;
            if (diff.sqrMagnitude < diff.sqrMagnitude)
            {
                var id = tgt.TargetInfo.TargetId;
                var request = new HeadQuaters.AddOrder.Request(id, new OrganizeOrder() { Customer = entityId.EntityId,
                                                                                          CustomerRank = rank,
                                                                                          Pos = pos.ToImprobableVector3(),
                                                                                          Side = side });
                Entity entity;
                if (TryGetEntity(id, out entity))
                {
                    commandSystem.SendCommand(request, entity);
                    action.ActionType = CommandActionType.Organize;
                }
            }
        }
    }
}
