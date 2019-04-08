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
            public ComponentDataArray<BaseUnitSight.Component> Sight;
            [ReadOnly] public ComponentDataArray<BaseUnitStatus.Component> Status;
            [ReadOnly] public ComponentArray<Transform> Transform;
        }

        [Inject] private Data data;

        protected override void OnUpdate()
        {
            for (var i = 0; i < data.Length; i++)
            {
                var movement = data.Movement[i];
                var sight = data.Sight[i];
                var status = data.Status[i];
                var pos = data.Transform[i].position;

                var time = Time.realtimeSinceStartup;
                if (time - sight.LastSearched < sight.Interval)
                    return;

                sight.LastSearched = time;
                data.Sight[i] = sight;

                var enemy = getNearestEnemeyPosition(status.Side, pos, sight.Range);
                var length = sight.Range * 0.2f;
                if (enemy == null || (pos - enemy.Value).sqrMagnitude < length * length)
                {
                    movement.IsTarget = false;
                    data.Movement[i] = movement;
                    continue;
                }


                movement.IsTarget = true;
                movement.TargetPosition = new Improbable.Vector3f( enemy.Value.x, enemy.Value.y, enemy.Value.z);
                data.Movement[i] = movement;
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
