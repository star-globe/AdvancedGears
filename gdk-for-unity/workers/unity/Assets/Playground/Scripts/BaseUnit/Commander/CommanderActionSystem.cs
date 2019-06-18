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
                        ProductAlly(trans.position, status.Side, commander, entityId, tgt, ref action);
                        break;

                    case OrderType.Organize:
                        OrganizeAlly(trans.position, status.Side, commander, entityId, tgt, ref action);
                        break;

                    default:
                        action.ActionType = CommandActionType.None;
                        break;
                }

                actionData[i] = action;
            }
        }

        void ProductAlly(in Vector3 pos, UnitSide side, in CommanderStatus.Component commander, in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetInfo.Position.ToUnityVector() - pos;
            float length = 10.0f;   // TODO from:master
            int num = 5;
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetInfo.TargetId;
            List<UnitFactory.AddFollowerOrder.Request> reqList = new List<UnitFactory.AddFollowerOrder.Request>();

            var n_sol = num - commander.FollowerInfo.Followers.Count;
            if (n_sol > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = num - commander.FollowerInfo.UnderCommanders.Count;
            if (n_com > 0 && commander.Rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = commander.Rank - 1 }));
            }

            Entity entity;
            if (TryGetEntity(id, out entity) == false)
                return;

            foreach(var r in reqList)
                commandSystem.SendCommand(r, entity);
            
            action.ActionType = CommandActionType.Product;
        }

        void OrganizeAlly(in Vector3 pos, UnitSide side, in CommanderStatus.Component commander, in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Organize)
                return;

            var diff = tgt.TargetInfo.Position.ToUnityVector() - pos;
            float length = 10.0f;   // TODO from:master
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetInfo.TargetId;
            var request = new HeadQuarters.AddOrder.Request(id, new OrganizeOrder() { Customer = entityId.EntityId,
                                                                                      CustomerRank = commander.Rank,
                                                                                      Pos = pos.ToImprobableVector3(),
                                                                                      Side = side });
            Entity entity;
            if (TryGetEntity(id, out entity) == false)
                return;

            commandSystem.SendCommand(request, entity);
            action.ActionType = CommandActionType.Organize;
        }
    }
}
