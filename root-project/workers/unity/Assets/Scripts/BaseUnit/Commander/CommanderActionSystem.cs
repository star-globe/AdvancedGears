using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class CommanderActionSystem : BaseCommanderSearch
    {
        private EntityQuery group;
        private IntervalChecker inter;
        private const int period = 10;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<CommanderAction.Component>(),
                ComponentType.ReadOnly<CommanderAction.HasAuthority>(),
                ComponentType.ReadOnly<CommanderTeam.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref CommanderAction.Component action,
                                          ref CommanderTeam.Component team,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (UnitUtils.IsOfficer(status.Type) == false)
                    return;

                if (!action.IsTarget)
                    return;

                //var trans = EntityManager.GetComponentObject<Transform>(entity);
                //ApplyOrder(trans.position, status.Side, status.Order, status.Rank, tgt);

                //switch (status.Order)
                //{
                //    case OrderType.Escape:
                //        ProductAlly(trans.position, status.Side, commander, team, entityId, tgt, ref action);
                //        break;
                //
                //    case OrderType.Organize:
                //        //
                //        break;
                //
                //    default:
                //        action.ActionType = CommandActionType.None;
                //        break;
                //}
            });
        }

        // boids commander
        void ApplyOrder(in Vector3 pos, UnitSide side, OrderType order, uint rank, in BaseUnitTarget.Component tgt)
        {
            var units = getAllyUnits(side, pos, RangeDictionary.Get(FixedRangeType.PlatoonRange), allowDead:false, UnitType.Soldier);
            foreach (var u in units) {
                SetCommand(u.id, new TargetInfo(tgt.TargetUnit, tgt.PowerRate), order);
            }
        }

        // add boids information
        private void SetCommand(EntityId id, in TargetInfo targetInfo, OrderType order)
        {
            base.SetOrder(id, order);
            this.UpdateSystem.SendEvent(new BaseUnitTarget.SetTarget.Event(targetInfo), id);
        }

        void ProductAlly(in Vector3 pos, UnitSide side,
                        uint rank, in CommanderTeam.Component team,
                        in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetUnit.Position.ToWorkerPosition(this.Origin) - pos;
            float length = 10.0f;   // TODO from:master
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetUnit.UnitId;
            List<UnitFactory.AddFollowerOrder.Request> reqList = new List<UnitFactory.AddFollowerOrder.Request>();

            var n_sol = 0;
            if (n_sol > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = 0;
            if (n_com > 0 && rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               //HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = rank - 1 }));
            }

            Entity entity;
            if (TryGetEntity(id, out entity) == false)
                return;

            foreach(var r in reqList)
                this.CommandSystem.SendCommand(r, entity);
            
            action.ActionType = CommandActionType.Product;
        }
    }
}
