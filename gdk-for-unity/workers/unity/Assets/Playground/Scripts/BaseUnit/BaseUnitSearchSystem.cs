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

                if (status.Act == ActState.Idle)
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

                action.EnemyPositions.Clear();

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;

                var enemy = getNearestEnemeyPosition(status.Side, pos, sight.Range);
                if (enemy == null)
                    enemy = getNearestEnemeyPosition(status.Side, pos, sight.Range * 3.0f, UnitType.Stronghold);
                else
                    action.IsTarget = true;

                if (enemy != null)
                {
                    movement.IsTarget = true;
                    var epos = new Improbable.Vector3f(enemy.Value.x - origin.x,
                                                        enemy.Value.y - origin.y,
                                                        enemy.Value.z - origin.z);
                    movement.TargetPosition = epos;
                    if (action.IsTarget)
                        action.EnemyPositions.Add(epos);
                }

                data.Movement[i] = movement;
                data.Action[i] = action;
            }
        }


    }

    public abstract class BaseSearchSystem : ComponentSystem
    {
        protected Vector3? getNearestEnemeyPosition(UnitSide self_side, Vector3 pos, float length, UnitType type = UnitType.None)
        {
            float len = float.MaxValue;
            Vector3? e_pos = null;

            var colls = Physics.OverlapSphere(pos, length, LayerMask.GetMask("Unit"));
            var worker = World.GetExistingManager<WorkerSystem>();
            for (var i = 0; i < colls.Length; i++)
            {
                var col = colls[i];
                var comp = col.GetComponent<LinkedEntityComponent>();
                if (comp == null)
                    continue;

                Entity entity;
                if (!worker.TryGetEntity(comp.EntityId, out entity))
                {
                    throw new InvalidOperationException(
                        $"Entity with SpatialOS Entity ID {comp.EntityId.Id} is not in this worker's view");
                }

                if (EntityManager.HasComponent<BaseUnitStatus.Component>(entity))
                {
                    var unit = EntityManager.GetComponentData<BaseUnitStatus.Component>(entity);
                    if (unit.State == UnitState.Dead)
                        continue;

                    if (unit.Side == self_side)
                        continue;

                    if (type != UnitType.None && type != unit.Type)
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
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
