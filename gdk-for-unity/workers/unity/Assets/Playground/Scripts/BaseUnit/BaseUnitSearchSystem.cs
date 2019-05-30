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
        ComponentGroup group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingManager<WorkerSystem>().Origin;

            group = GetComponentGroup(
                ComponentType.Create<BaseUnitMovement.Component>(),
                ComponentType.Create<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.Create < BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            var movementData = group.GetComponentDataArray<BaseUnitMovement.Component>();
            var actionData = group.GetComponentDataArray<BaseUnitAction.Component>();
            var sightData = group.GetComponentDataArray<BaseUnitSight.Component>();
            var statusData = group.GetComponentDataArray<BaseUnitStatus.Component>();
            var targetData = group.GetComponentDataArray<BaseUnitTarget.Component>();
            var transData = group.GetComponentArray<Transform>();

            for (var i = 0; i < movementData.Length; i++)
            {
                var movement = movementData[i];
                var action = actionData[i];
                var sight = sightData[i];
                var status = statusData[i];
                var target = targetData[i];
                var pos = transData[i].position;

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
                sightData[i] = sight;

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;
                action.EnemyPositions.Clear();

                UnitInfo enemy = null;
                if (status.Order != OrderType.Escape)
                    enemy = getNearestEnemey(status.Side, pos, sight.Range);

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
                if (status.Type == UnitType.Commander && target.TargetInfo.Side != status.Side)
                {
                    switch (target.TargetInfo.Type)
                    {
                        case UnitType.Commander:
                        case UnitType.Stronghold: range += target.TargetInfo.AllyRange; break;
                    }
                }

                switch (status.Order)
                {
                    case OrderType.Move:
                    case OrderType.Escape:  range = 0.2f; break;
                    case OrderType.Attack:  range *= 0.8f; break;
                    case OrderType.Escape:  range *= 1.6f; break;
                    case OrderType.Keep:    range *= 1.0f; break;
                }

                movement.TargetRange = range;

                movementData[i] = movement;
                actionData[i] = action;
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

        protected UnitInfo getNearestEnemey(UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, true, types);
        }

        protected UnitInfo getNearestAlly(UnitSide self_side, in Vector3 pos, float length, params UnitType[] types)
        {
            return getNearestUnit(self_side, pos, length, false, types);
        }

        protected UnitInfo getNearestUnit(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, params UnitType[] types)
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

                    if ((unit.Value.Side == self_side) == isEnemy)
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
                        info.side = unit.Value.Side;
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

        protected bool TryGetEntity(EntityId id, out Entity entity)
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
        public UnitSide side;
    }

    public static class RandomInterval
    {
        public static float GetRandom(float inter)
        {
            return inter * 0.1f * UnityEngine.Random.Range(-1.0f, 1.0f);
        }
    }
}
