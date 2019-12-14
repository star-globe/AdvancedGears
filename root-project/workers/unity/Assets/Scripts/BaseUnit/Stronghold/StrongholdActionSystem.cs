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
    internal class StrongholdActionSystem : BaseSearchSystem
    {
        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<StrongholdUnitStatus.Component>(),
                ComponentType.ReadOnly<StrongholdUnitStatus.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(StrongholdUnitStatus.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref StrongholdUnitStatus.Component stronghold,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component tgt,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var inter = stronghold.Interval;
                if (inter.CheckTime() == false)
                    return;

                stronghold.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                switch (status.Order)
                {
                    case OrderType.Attack:
                        CheckAttackers(trans.position, status.Side, ref stronghold);
                        break;
                
                    case OrderType.Guard:
                        //
                        break;

                    case OrderType.Supply:
                        break;
                
                    default:
                        //action.ActionType = CommandActionType.None;
                        break;
                }
            });
        }

        void CheckAttackers(in Vector3 pos, UnitSide side,
                        ref StrongholdUnitStatus.Component stronghold)
        {
            var datas = stronghold.CommanderDatas;
            var removeList = new List<EntityId>();
            foreach (var kvp in datas)
            {
                if (CheckAlive(kvp.Key.Id) == false)
                    removeList.Add(kvp.Key);
            }

            if (removeList.Count > 0) {
                foreach (var r in removeList)
                    datas.Remove(r);

                stronghold.CommanderDatas = datas;
            }


        }

        void ProductAlly(in Vector3 pos, UnitSide side,
                        in CommanderStatus.Component commander, in CommanderTeam.Component team,
                        in SpatialEntityId entityId, in BaseUnitTarget.Component tgt, ref CommanderAction.Component action)
        {
            if (action.ActionType == CommandActionType.Product)
                return;

            var diff = tgt.TargetInfo.Position.ToWorkerPosition(this.Origin) - pos;
            float length = 10.0f;   // TODO from:master
            if (diff.sqrMagnitude > length * length)
                return;

            var id = tgt.TargetInfo.TargetId;
            List<UnitFactory.AddFollowerOrder.Request> reqList = new List<UnitFactory.AddFollowerOrder.Request>();

            var n_sol = 0;// commander.TeamConfig.Soldiers - GetFollowerCount(team, false);
            if (n_sol > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_sol,
                                                                                               Type = UnitType.Soldier,
                                                                                               Side = side }));
            }

            var n_com = 0;// commander.TeamConfig.Commanders - GetFollowerCount(team,true);
            if (n_com > 0 && commander.Rank > 0) {
                reqList.Add(new UnitFactory.AddFollowerOrder.Request(id, new FollowerOrder() { Customer = entityId.EntityId,
                                                                                               HqEntityId = team.HqInfo.EntityId,
                                                                                               Number = n_com,
                                                                                               Type = UnitType.Commander,
                                                                                               Side = side,
                                                                                               Rank = commander.Rank - 1 }));
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
