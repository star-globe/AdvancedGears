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
        private struct Data
        {
            public readonly int Length;
            public ComponentDataArray<CommanderSight.Component> Sight;
            public ComponentDataArray<CommanderStatus.Component> CommanderStatus;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> BaseUnitStatus;
            [ReadOnly] public ComponentArray<Transform> Transform;
            [ReadOnly] public ComponentDataArray<SpatialEntityId> EntityId;
        }

        [Inject] private Data data;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;
        }

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var sight = data.Sight[i];
                var commander = data.CommanderStatus[i];
                var status = data.BaseUnitStatus[i];
                var pos = data.Transform[i].position;
                var entityId = data.EntityId[i];

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Type != UnitType.Commander)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = sight.Interval;
                if (time - sight.LastSearched < inter)
                    continue;

                sight.LastSearched = time + RandomInterval.GetRandom(inter);

                var tgt = getNearestEnemeyPosition(status.Side, pos, sight.Range, UnitType.Stronghold);
                sight.IsTarget = tgt != null;
                var tpos = Improbable.Vector3f.Zero;
                if (sight.IsTarget)
                {
                    tpos = new Improbable.Vector3f(tgt.Value.x - origin.x,
                                                   tgt.Value.y - origin.y,
                                                   tgt.Value.z - origin.z);
                }

                sight.TargetPosition = tpos;

                // check 
                OrderType current = GetOrder(status.Side, pos, sight.Range);

                commander.SelfOrder = current;

                SetFollowers(commander.FollowerInfo.Followers,
                             new TargetInfo (sight.IsTarget,
                                             sight.TargetPosition,
                                             entityId.EntityId,
                                             commander.AllyRange),
                             current);

                data.Sight[i] = sight;
                data.CommanderStatus[i] = commander;
            }
        }

        private OrderType GetOrder(UnitSide side, Vector3 pos, float length)
        {
            float len = float.MaxValue;
            Vector3? e_pos = null;

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

            float rate = 1.1f;
            if (ally > enemy * rate)
                return OrderType.Attack;
            
            if (ally * rate * rate < enemy)
                return OrderType.Escape;

            return OrderType.Keep;
        }

        private void SetFollowers(List<EntityId> followers, TargetInfo targetInfo, OrderType order)
        {
            foreach (var id in followers)
            {
                BaseUnitTarget.CommandSenders.SetTarget? tgtSender;
                if (base.TryGetComponent(id, out tgtSender))
                {
                    var request = new BaseUnitTarget.SetTarget.Request(
                        id,
                        targetInfo);
                    tgtSender.Value.RequestsToSend.Add(request);
                    base.SetComponent(id, tgtSender.Value);
                }

                BaseUnitStatus.Component? status;
                if (base.TryGetComponent(id, out status) == false)
                    continue;

                if (status.Value.Order == order)
                    continue;

                BaseUnitStatus.CommandSenders.SetOrder? orderSender;
                if (base.TryGetComponent(id, out orderSender))
                {
                    var request = new BaseUnitStatus.SetOrder.Request(
                        id,
                        new OrderInfo()
                        {
                            Order = order,
                        });
                    orderSender.Value.RequestsToSend.Add(request);
                    base.SetComponent(id, orderSender.Value);
                }
            }
        }
    }
}
