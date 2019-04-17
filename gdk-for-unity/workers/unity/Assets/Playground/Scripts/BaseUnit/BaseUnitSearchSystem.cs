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
    public class BaseUnitSearchSystem : BaseSearchSystem
    {
        private struct Data
        {
            public readonly int Length;
            public ComponentDataArray<BaseUnitMovement.Component> Movement;
            public ComponentDataArray<BaseUnitAction.Component> Action;
            public ComponentDataArray<BaseUnitSight.Component> Sight;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            [ReadOnly] public ComponentArray<Transform> Transform;
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
                var movement = data.Movement[i];
                var action = data.Action[i];
                var sight = data.Sight[i];
                var status = data.Status[i];
                var pos = data.Transform[i].position;

                if (status.State != UnitState.Alive)
                    continue;

                if (status.Order == OrderType.Idle)
                    continue;

                if (status.Type != UnitType.Soldier &&
                    status.Type != UnitType.Commander)
                    continue;

                var time = Time.realtimeSinceStartup;
                var inter = sight.Interval;
                if (time - sight.LastSearched < inter)
                    continue;

                sight.LastSearched = time + RandomInterval.GetRandom(inter);
                data.Sight[i] = sight;

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;
                action.EnemyPositions.Clear();

                var enemy = getNearestEnemeyPosition(status.Side, pos, sight.Range);
                if (enemy == null)
                {
                    if (movement.SetTarget)
                    {
                        movement.TargetPosition = movement.OrderedPosition;
                        movement.IsTarget = true;
                    }
                }
                else
                {
                    movement.IsTarget = true;
                    action.IsTarget = true;
                    var epos = new Improbable.Vector3f(enemy.Value.x - origin.x,
                                                       enemy.Value.y - origin.y,
                                                       enemy.Value.z - origin.z);
                    movement.TargetPosition = epos;
                    action.EnemyPositions.Add(epos);
                }

                data.Movement[i] = movement;
                data.Action[i] = action;
            }
        }
    }

    public abstract class BaseSearchSystem : ComponentSystem
    {
        WorkerSystem worker = null;
        WorkerSystem Worker
        {
            get
            {
                worker = worker ?? World.GetExistingManager<WorkerSystem>();
                return worker;
            }
        }

        protected Vector3? getNearestEnemeyPosition(UnitSide self_side, Vector3 pos, float length, UnitType type = UnitType.None)
        {
            float len = float.MaxValue;
            Vector3? e_pos = null;

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

                    if (unit.Value.Side == self_side)
                        continue;

                    if (type != UnitType.None && type != unit.Value.Type)
                        continue;

                    var t_pos = col.transform.position;
                    var l = (t_pos - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        e_pos = t_pos;
                    }
                }
            }

            return e_pos;
        }

        protected bool TryGetComponent<T>(EntityId id, out T? comp) where T : struct, IComponentData
        {
            comp = null;
            Entity entity;
            this.TryGetEntity(id, out entity);

            if (EntityManager.HasComponent<T>(entity))
            {
                comp = EntityManager.GetComponentData<T>(entity);
                return true;
            }
            else
                return false;
        }

        protected void SetComponent<T>(EntityId id, T comp) where T: struct, IComponentData
        {
            Entity entity;
            this.TryGetEntity(id, out entity);

            if (EntityManager.HasComponent<T>(entity))
                EntityManager.SetComponentData(entity, comp);
        }

        private void TryGetEntity(EntityId id, out Entity entity)
        {
            if (!this.Worker.TryGetEntity(id, out entity))
            {
                throw new InvalidOperationException(
                    $"Entity with SpatialOS Entity ID {id} is not in this worker's view");
            }
        }
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
