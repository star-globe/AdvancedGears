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
    internal class CommanderActionSystem : BaseSearchSystem
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
                ComponentType.ReadWrite<CommanderAction.Component>(),
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
            Entities.With(group).ForEach((Entity entity,
                                          ref CommanderAction.Component action,
                                          ref CommanderStatus.Component commander,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Commander)
                    return;

                if (!action.IsTarget)
                    return;

                var time = Time.realtimeSinceStartup;
                var inter = action.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                action.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
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
            });
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
