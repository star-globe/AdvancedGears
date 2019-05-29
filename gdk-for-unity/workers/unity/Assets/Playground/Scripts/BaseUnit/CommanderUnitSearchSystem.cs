using System;
using System.Collections;
using System.Collections.Generic;
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
    public class CommanderUnitSearchSystem : BaseSearchSystem
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
                ComponentType.Create<CommanderSight.Component>(),
                ComponentType.Create<CommanderStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            var sightData = group.GetComponentDataArray<CommanderSight.Component>();
            var commanderData = group.GetComponentDataArray<CommanderStatus.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var transData = group.GetComponentArray<Transform>();
            var entityIdData = group.GetComponentDataArray<SpatialEntityId>();

            for (var i = 0; i < sightData.Length; i++)
            {
                var sight = sightData[i];
                var commander = commanderData[i];
                var status = statusData[i];
                var pos = transData[i].position;
                var entityId = entityIdData[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = sight.Interval;
                if (inter.CheckTime(time) == false)
                    continue;

                sight.Interval = inter;

                if (commander.FollowerInfo.Followers.Count == 0)
                {
                    // Search Stronghold and Get Followers
                    GetFollowers();
                }
                else
                {
                    AttackEnemy(status, entityId, pos, ref sight, ref commander);
                }

                sightData[i] = sight;
                commanderData[i] = commander;
            }
        }

        void GetFollowers()
        {
        }

        void AttackEnemy(BaseUnitStatus.Component status, SpatialEntityId entityId, Vector3 pos, ref CommanderSight.Component sight, ref CommanderStatus.Component commander)
        {
            var tgt = getNearestEnemey(status.Side, pos, sight.Range, UnitType.Stronghold, UnitType.Commander);
            sight.IsTarget = tgt != null;
            var tpos = Improbable.Vector3f.Zero;
            var type = UnitType.None;
            if (sight.IsTarget)
            {
                tpos = new Improbable.Vector3f(tgt.pos.x - origin.x,
                                               tgt.pos.y - origin.y,
                                               tgt.pos.z - origin.z);
                type = tgt.type;
            }

            sight.TargetPosition = tpos;
            sight.TargetType = type;

            // check 
            OrderType current = GetOrder(status.Side, pos, sight.Range);

            commander.SelfOrder = current;

            var targetInfo = new TargetInfo(sight.IsTarget,
                                         sight.TargetPosition,
                                         sight.TargetType,
                                         entityId.EntityId,
                                         commander.AllyRange);
            SetFollowers(commander.FollowerInfo.Followers, ref targetInfo, current);
        }


        private OrderType GetOrder(UnitSide side, Vector3 pos, float length)
        {
            float ally = 0.0f;
            float enemy = 0.0f;

            var colls = Physics.OverlapSphere(pos, length, LayerMask.GetMask("Unit"));
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                BaseUnitStatus.Component? unit;
                if (TryGetComponent(comp.EntityId, out unit))
                {
                    if (unit.Value.State == UnitState.Dead)
                        continue;

                    if (unit.Value.Side == side)
                        ally += 1.0f;
                    else
                        enemy += 1.0f;
                }
            }

            float rate = 1.3f;
            if (ally > enemy * rate)
                return OrderType.Attack;
            
            if (ally * rate * rate < enemy)
                return OrderType.Escape;

            return OrderType.Keep;
        }

        private void SetFollowers(List<EntityId> followers, ref TargetInfo targetInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                SetCommand(id, ref targetInfo, order);
            }

            SetCommand(targetInfo.CommanderId, ref targetInfo, order);
        }

        private bool SetCommand(EntityId id, ref TargetInfo targetInfo, OrderType order)
        {
            Entity entity;
            if (!base.TryGetEntity(id, out entity))
                return false;

            commandSystem.SendCommand(new BaseUnitTarget.SetTarget.Request(id, targetInfo), entity);

            BaseUnitStatus.Component? status;
            if (base.TryGetComponent(id, out status) == false)
                return false;

            if (status.Value.Order == order)
                return false;

            commandSystem.SendCommand(new BaseUnitStatus.SetOrder.Request(id, new OrderInfo(){ Order = order }), entity);
            return true;
        }
    }
}
