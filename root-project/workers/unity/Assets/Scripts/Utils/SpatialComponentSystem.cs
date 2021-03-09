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

namespace AdvancedGears
{
    public abstract class SpatialComponentBaseSystem : ComponentSystem
    {
        WorkerSystem worker = null;
        protected WorkerSystem WorkerSystem
        {
            get
            {
                worker = worker ?? World.GetExistingSystem<WorkerSystem>();
                return worker;
            }
        }

        CommandSystem command = null;
        protected CommandSystem CommandSystem
        {
            get
            {
                command = command ?? World.GetExistingSystem<CommandSystem>();
                return command;
            }
        }

        ComponentUpdateSystem update = null;
        protected ComponentUpdateSystem UpdateSystem
        {
            get
            {
                update = update ?? World.GetExistingSystem<ComponentUpdateSystem>();
                return update;
            }
        }

        private Vector3? origin = null;
        protected Vector3 Origin
        {
            get
            {
                if (this.WorkerSystem == null)
                    return Vector3.zero;

                origin = origin ?? this.WorkerSystem.Origin;
                return origin.Value;
            }
        }

        ILogDispatcher logDispatcher = null;
        protected ILogDispatcher LogDispatcher
        {
            get
            {
                if (this.WorkerSystem == null)
                    return null;

                logDispatcher = logDispatcher ?? this.WorkerSystem.LogDispatcher;
                return logDispatcher;
            }
        }
    }

    public abstract class SpatialComponentSystem : SpatialComponentBaseSystem
    {
        LocalTimerUpdateSystem localTimerSystem = null;
        protected LocalTimerUpdateSystem LocalTimerSystem
        {
            get
            {
                localTimerSystem = localTimerSystem ?? World.GetExistingSystem<LocalTimerUpdateSystem>();
                return localTimerSystem;
            }
        }

        protected double CurrentTime
        {
            get
            {
                if (this.LocalTimerSystem == null)
                    return (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0;

                return this.LocalTimerSystem.CurrentTime;
            }
        }

        protected bool CheckTime(ref IntervalChecker inter)
        {
            return inter.CheckTime(this.CurrentTime);
        }

        protected void UpdateLastChecked(ref IntervalChecker inter)
        {
            inter.UpdateLastChecked(this.CurrentTime);
        }
    }

    public abstract class BaseEntitySearchSystem : SpatialComponentSystem
    {
        protected bool TryGetComponentObject<T>(EntityId id, out T comp) where T : UnityEngine.Component
        {
            comp = null;
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return false;

            comp = EntityManager.GetComponentObject<T>(entity);
            return comp != null;
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

        protected bool HasComponent<T>(in EntityId id) where T : struct, IComponentData
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return false;

            return EntityManager.HasComponent<T>(entity);
        }

        protected void AddComponent<T>(in Entity entity, T comp) where T : struct, IComponentData
        {
            if (EntityManager.HasComponent<T>(entity))
                EntityManager.SetComponentData(entity, comp);
            else
                EntityManager.AddComponentData(entity, comp);
        }

        protected void AddComponent<T>(EntityId id, T comp) where T : struct, IComponentData
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return;

            AddComponent(entity, comp);
        }

        protected void SetComponent<T>(in Entity entity, T comp) where T : struct, IComponentData
        {
            if (EntityManager.HasComponent<T>(entity))
                PostUpdateCommands.SetComponent(entity, comp);
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

        protected void RemoveComponent(EntityId id, ComponentType compType)
        {
            Entity entity;
            if (!this.TryGetEntity(id, out entity))
                return;

            RemoveComponent(entity, compType);
        }

        protected bool TryGetEntity(EntityId id, out Entity entity)
        {
            if (!this.WorkerSystem.TryGetEntity(id, out entity))
                return false;

            return true;
        }

        protected bool HasEntity(EntityId id)
        {
            return this.WorkerSystem.HasEntity(id);
        }
    }
}
