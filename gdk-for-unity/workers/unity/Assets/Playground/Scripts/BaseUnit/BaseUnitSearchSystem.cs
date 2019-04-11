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
    internal class BaseUnitSearchSystem : ComponentSystem
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

                var time = Time.realtimeSinceStartup;
                if (time - sight.LastSearched < sight.Interval)
                    continue;

                sight.LastSearched = time;
                data.Sight[i] = sight;

                action.EnemyPositions.Clear();

                var enemy = getNearestEnemeyPosition(status.Side, pos, sight.Range);
                var length = sight.Range;// * 0.2f;
                if (enemy == null || (pos - enemy.Value).sqrMagnitude > length * length)
                {
                    movement.IsTarget = false;
                    action.IsTarget = false;
                    data.Movement[i] = movement;
                    data.Action[i] = action;
                    continue;
                }

                movement.IsTarget = true;
                var epos = new Improbable.Vector3f( enemy.Value.x - origin.x,
                                                    enemy.Value.y - origin.y,
                                                    enemy.Value.z - origin.z);
                movement.TargetPosition = epos;
                action.IsTarget = true;
                action.EnemyPositions.Add(epos);
                data.Movement[i] = movement;
                data.Action[i] = action;
            }
        }

        Vector3? getNearestEnemeyPosition(uint self_side, Vector3 pos, float length)
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
                    if (unit.Side == self_side)
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
}
