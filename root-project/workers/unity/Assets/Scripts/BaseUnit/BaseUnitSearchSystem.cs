using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
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
    public class BaseUnitSearchSystem : BaseSearchSystem
    {
        EntityQuery group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.ReadWrite<BaseUnitSight.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                ComponentType.ReadOnly<GunComponent.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitAction.Component action,
                                          ref BaseUnitSight.Component sight,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component target,
                                          ref GunComponent.Component gun) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                if (status.Type != UnitType.Soldier &&
                    status.Type != UnitType.Commander)
                    return;

                var time = Time.time;
                var inter = sight.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                sight.Interval = inter;

                // initial
                movement.IsTarget = false;
                action.IsTarget = false;
                action.EnemyPositions.Clear();

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

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

                var range = gun.GetAttackRange();
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
                    case OrderType.Escape: range *= 0.4f; break;
                    case OrderType.Attack: range *= 0.8f; break;
                    case OrderType.Keep: range *= 1.0f; break;
                }

                movement.TargetRange = range;
            });
        }
    }

    public abstract class BaseSearchSystem : BaseEntitySearchSystem
    {
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
                        info.id = comp.EntityId;
                        info.pos = t_pos;
                        info.type = unit.Value.Type;
                        info.side = unit.Value.Side;
                    }
                }
            }

            return info;
        }

        protected List<UnitInfo> getUnits(UnitSide self_side, in Vector3 pos, float length, bool isEnemy, bool allowDead, params UnitType[] types)
        {
            List<UnitInfo> unitList = new List<UnitInfo>();

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
                    if (unit.Value.State == UnitState.Dead && allowDead == false)
                        continue;

                    if ((unit.Value.Side == self_side) == isEnemy)
                        continue;

                    if (types.Length != 0 && types.Contains(unit.Value.Type) == false)
                        continue;

                    var info = new UnitInfo();
                    info.id = comp.EntityId;
                    info.pos = col.transform.position;
                    info.type = unit.Value.Type;
                    info.side = unit.Value.Side;

                    unitList.Add(info);
                }
            }

            return unitList;
        }
    }

    public abstract class BaseEntitySearchSystem : ComponentSystem
    {
        WorkerSystem worker = null;
        protected WorkerSystem Worker
        {
            get
            {
                worker = worker ?? World.GetExistingSystem<WorkerSystem>();
                return worker;
            }
        }

        CommandSystem command = null;
        protected CommandSystem Command
        {
            get
            {
                command = command ?? World.GetExistingSystem<CommandSystem>();
                return command;
            }
        }

        protected bool TryGetComponent<T>(in Entity entity, out T? comp) where T : struct, IComponentData
        {
            comp = null;
            if (EntityManager.HasComponent<T>(entity))
            {
                comp = EntityManager.GetComponentData<T>(entity);
                return true;
            }
            else
                return false;
        }

        protected bool TryGetComponent<T>(EntityId id, out T? comp) where T : struct, IComponentData
        {
            comp = null;
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return false;

            return TryGetComponent(entity, out comp);
        }

        protected void SetComponent<T>(in Entity entity, T comp) where T : struct, IComponentData
        {
            if (EntityManager.HasComponent<T>(entity))
                EntityManager.SetComponentData(entity, comp);
        }

        protected void SetComponent<T>(EntityId id, T comp) where T : struct, IComponentData
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return;

            SetComponent(entity, comp);
        }

        protected void RemoveComponent(in Entity entity, ComponentType compType)
        {
            if (EntityManager.HasComponent(entity, compType))
                EntityManager.RemoveComponent(entity, compType);
        }

        protected void RemoveComponent(EntityId id,  ComponentType compType)
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return;

            RemoveComponent(entity, compType);
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
        public EntityId id;
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
