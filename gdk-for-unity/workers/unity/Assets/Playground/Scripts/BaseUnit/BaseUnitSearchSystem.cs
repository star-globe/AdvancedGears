using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
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
            [ReadOnly] public ComponentDataArray<BaseUnitTarget.Component> Target;
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
                var target = data.Target[i];
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
                if (inter.CheckTime(time) == false)
                    continue;

                sight.Interval = inter;
                data.Sight[i] = sight;

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;
                action.EnemyPositions.Clear();

                var enemy = getNearestEnemey(status.Side, pos, sight.Range);
                if (enemy == null)
                {
                    if (target.TargetInfo.IsTarget)
                    {
                        movement.TargetPosition = target.TargetInfo.Position;
                        movement.IsTarget = true;
                    }
                }
                else
                {
                    movement.IsTarget = true;
                    action.IsTarget = true;
                    var epos = new Improbable.Vector3f(enemy.pos.x - origin.x,
                                                       enemy.pos.y - origin.y,
                                                       enemy.pos.z - origin.z);
                    movement.TargetPosition = epos;
                    action.EnemyPositions.Add(epos);
                }

                var entityId = target.TargetInfo.CommanderId;
                Position.Component? comp = null;
                if (entityId.IsValid() && base.TryGetComponent<Position.Component>(entityId, out comp))
                {
                    movement.CommanderPosition = new Vector3f((float)comp.Value.Coords.X,
                                                              (float)comp.Value.Coords.Y,
                                                              (float)comp.Value.Coords.Z);
                }
                else
                    movement.CommanderPosition = Vector3f.Zero;

                var range = action.AttackRange;
                if (status.Type == UnitType.Commander)
                {
                    switch (target.TargetInfo.Type)
                    {
                        case UnitType.Commander:
                        case UnitType.Stronghold: range += target.TargetInfo.AllyRange; break;
                    }
                }

                switch (status.Order)
                {
                    case OrderType.Move: range = 0.2f; break;
                    case OrderType.Attack: range *= 0.8f; break;
                    case OrderType.Escape: range *= 1.6f; break;
                    case OrderType.Keep: range *= 1.0f; break;
                }
                movement.TargetRange = range;

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

        protected UnitInfo getNearestEnemey(UnitSide self_side, Vector3 pos, float length, params UnitType[] types)
        {
            float len = float.MaxValue;
            UnitInfo info = null;

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

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var t_pos = col.transform.position;
                    var l = (t_pos - pos).sqrMagnitude;
                    if (l < len)
                    {
                        len = l;
                        info = info ?? new UnitInfo();
                        info.pos = t_pos;
                        info.type = unit.Value.Type;
                    }
                }
            }

            return info;
        }

        protected bool TryGetComponent<T>(EntityId id, out T? comp) where T : struct, IComponentData
        {
            comp = null;
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return false;

            if (EntityManager.HasComponent<T>(entity))
            {
                comp = EntityManager.GetComponentData<T>(entity);
                return true;
            }
            else
                return false;
        }

        protected void SetComponent<T>(EntityId id, T comp) where T : struct, IComponentData
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return;

            if (EntityManager.HasComponent<T>(entity))
                EntityManager.SetComponentData(entity, comp);
        }

        private bool TryGetEntity(EntityId id, out Entity entity)
        {
            if (!this.Worker.TryGetEntity(id, out entity))
            {
                Debug.LogError($"Entity with SpatialOS Entity ID {id} is not in this worker's view");
                return false;
            }

            return true;
        }
    }

    // Utils
    public class UnitInfo
    {
        public Vector3 pos;
        public UnitType type;
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }

    public static class RotateLogic
    {
        public static void Rotate(Transform trans, Vector3 foward, float angle = float.MaxValue)
        {
            Rotate(trans, trans.up, foward, angle, false);
        }

        public static void Rotate(Transform trans, Vector3 up, Vector3 foward, float angle = float.MaxValue, bool fit = true)
        {
            var dot = Vector3.Dot(up, foward);
            foward -= dot * up;
            foward.Normalize();

            var deg = angle != float.MaxValue ? angle * Mathf.Rad2Deg : float.MaxValue;
            var axis = Vector3.Cross(trans.forward, foward);
            var ang = Vector3.Angle(trans.forward, foward);
            if (ang < deg)
                deg = ang;

            var u = up;
            if (Vector3.Dot(axis, up) < 0)
                u = -up;

            var q = Quaternion.AngleAxis(deg, u);
            var nq = trans.rotation * q;
            trans.rotation = nq;

            if (fit)
                trans.rotation = Quaternion.LookRotation(trans.forward, up);
        }

        public static bool CheckRotate(Transform trans, Vector3 up, Vector3 foward, float angle)
        {
            var dot = Vector3.Dot(up, foward);
            foward -= dot * up;
            foward.Normalize();

            var d = Vector3.Dot(foward, trans.forward);
            return d > Mathf.Cos(angle);
        }
    }
}
