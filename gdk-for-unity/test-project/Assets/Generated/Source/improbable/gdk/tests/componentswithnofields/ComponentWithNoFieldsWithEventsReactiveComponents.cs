// ===========
// DO NOT EDIT - this file is automatically regenerated.
// ===========

#if !DISABLE_REACTIVE_COMPONENTS
using System.Collections.Generic;
using Unity.Entities;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Worker.CInterop;

namespace Improbable.Gdk.Tests.ComponentsWithNoFields
{
    public partial class ComponentWithNoFieldsWithEvents
    {
        public class UpdateReactiveComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var updatesReceived = updateSystem.GetComponentUpdatesReceived<Update>();

                for (int i = 0; i < updatesReceived.Count; ++i)
                {
                    ref readonly var update = ref updatesReceived[i];
                    if (!workerSystem.TryGetEntity(update.EntityId, out var entity))
                    {
                        continue;
                    }

                    List<Update> updates;
                    if (entityManager.HasComponent<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.ReceivedUpdates>(entity))
                    {
                        updates = entityManager.GetComponentData<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.ReceivedUpdates>(entity).Updates;
                    }
                    else
                    {
                        updates = Update.Pool.Count > 0 ? Update.Pool.Pop() : new List<Update>();
                        var updatesComponent = new ReceivedUpdates
                        {
                            handle = ReferenceTypeProviders.UpdatesProvider.Allocate(world)
                        };
                        ReferenceTypeProviders.UpdatesProvider.Set(updatesComponent.handle, updates);
                        entityManager.AddComponentData(entity, updatesComponent);
                    }

                    updates.Add(update.Update);
                }
            }

            public void Clean(World world)
            {
                global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.ReferenceTypeProviders.UpdatesProvider.CleanDataInWorld(world);
            }
        }

        public class AddComponentReactiveComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var added = updateSystem.GetComponentsAdded(ComponentId);

                foreach (var entityId in added)
                {
                    if (!workerSystem.TryGetEntity(entityId, out var entity))
                    {
                        continue;
                    }

                    if (entityManager.HasComponent<ComponentRemoved<Component>>(entity))
                    {
                        entityManager.RemoveComponent<ComponentRemoved<Component>>(entity);
                    }

                    entityManager.AddComponent(entity, ComponentType.Create<ComponentAdded<Component>>());
                }
            }

            public void Clean(World world)
            {
            }
        }

        public class RemoveComponentReactiveComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var removed = updateSystem.GetComponentsRemoved(ComponentId);

                foreach (var entityId in removed)
                {
                    if (!workerSystem.TryGetEntity(entityId, out var entity))
                    {
                        continue;
                    }

                    if (entityManager.HasComponent<ComponentAdded<Component>>(entity))
                    {
                        entityManager.RemoveComponent<ComponentAdded<Component>>(entity);
                    }

                    entityManager.AddComponent(entity, ComponentType.Create<ComponentRemoved<Component>>());
                }
            }

            public void Clean(World world)
            {
            }
        }

        public class AuthorityReactiveComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var authorityChanges = updateSystem.GetAuthorityChangesReceived(ComponentId);

                for (int i = 0; i < authorityChanges.Count; ++i)
                {
                    ref readonly var auth = ref authorityChanges[i];
                    if (!workerSystem.TryGetEntity(auth.EntityId, out var entity))
                    {
                        continue;
                    }

                    List<Authority> changes;
                    if (entityManager.HasComponent<AuthorityChanges<Component>>(entity))
                    {
                        changes = entityManager.GetComponentData<AuthorityChanges<Component>>(entity).Changes;
                    }
                    else
                    {
                        changes = new List<Authority>();
                        var authComponent = new AuthorityChanges<Component>
                        {
                            Handle = AuthorityChangesProvider.Allocate(world)
                        };
                        AuthorityChangesProvider.Set(authComponent.Handle, changes);
                        entityManager.AddComponentData(entity, authComponent);
                    }

                    changes.Add(auth.Authority);
                }
            }

            public void Clean(World world)
            {
                AuthorityChangesProvider.CleanDataInWorld(world);
            }
        }


        public class EvtEventReactiveComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var eventsReceived = updateSystem.GetEventsReceived<Evt.Event>();

                for (int i = 0; i < eventsReceived.Count; ++i)
                {
                    ref readonly var ev = ref eventsReceived[i];
                    if (!workerSystem.TryGetEntity(ev.EntityId, out var entity))
                    {
                        continue;
                    }

                    List<global::Improbable.Gdk.Tests.ComponentsWithNoFields.Empty> events;
                    if (entityManager.HasComponent<ReceivedEvents.Evt>(entity))
                    {
                        events = entityManager.GetComponentData<ReceivedEvents.Evt>(entity).Events;
                    }
                    else
                    {
                        events = new List<global::Improbable.Gdk.Tests.ComponentsWithNoFields.Empty>();
                        var eventsComponent = new ReceivedEvents.Evt
                        {
                            handle = ReferenceTypeProviders.EvtProvider.Allocate(world)
                        };
                        ReferenceTypeProviders.EvtProvider.Set(eventsComponent.handle, events);
                        entityManager.AddComponentData(entity, eventsComponent);
                    }

                    events.Add(ev.Event.Payload);
                }

                var authorityChanges = updateSystem.GetAuthorityChangesReceived(ComponentId);

                for (int i = 0; i < authorityChanges.Count; ++i)
                {
                    ref readonly var auth = ref authorityChanges[i];
                    if (!workerSystem.TryGetEntity(auth.EntityId, out var entity))
                    {
                        continue;
                    }

                    // Add event senders
                    if (auth.Authority == Authority.Authoritative)
                    {
                        var eventSender = new EventSender.Evt()
                        {
                            handle = ReferenceTypeProviders.EvtProvider.Allocate(world)
                        };
                        ReferenceTypeProviders.EvtProvider.Set(eventSender.handle, new List<global::Improbable.Gdk.Tests.ComponentsWithNoFields.Empty>());
                        entityManager.AddComponentData(entity, eventSender);
                    }

                    // Remove event senders
                    if (auth.Authority == Authority.NotAuthoritative)
                    {
                        var eventSender = entityManager.GetComponentData<EventSender.Evt>(entity);
                        ReferenceTypeProviders.EvtProvider.Free(eventSender.handle);
                        entityManager.RemoveComponent<EventSender.Evt>(entity);
                    }
                }
            }

            public void Clean(World world)
            {
                global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.ReferenceTypeProviders.EvtProvider.CleanDataInWorld(world);
            }
        }

        public class LegacyAuthorityComponentManager : IReactiveComponentManager
        {
            public void PopulateReactiveComponents(ComponentUpdateSystem updateSystem, EntityManager entityManager, WorkerSystem workerSystem, World world)
            {
                var authorityChanges = updateSystem.GetAuthorityChangesReceived(ComponentId);

                foreach (var entityId in world.GetExistingManager<EntitySystem>().GetEntitiesAdded())
                {
                    workerSystem.TryGetEntity(entityId, out var entity);
                    entityManager.AddComponent(entity,
                        ComponentType.Create<NotAuthoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>());
                }

                for (int i = 0; i < authorityChanges.Count; ++i)
                {
                    ref readonly var auth = ref authorityChanges[i];
                    if (!workerSystem.TryGetEntity(auth.EntityId, out var entity))
                    {
                        continue;
                    }

                    ApplyAuthorityChange(entity, auth.Authority, auth.EntityId, world, entityManager);
                }
            }

            public void Clean(World world)
            {
            }

            private void ApplyAuthorityChange(Unity.Entities.Entity entity, Authority authority, global::Improbable.Gdk.Core.EntityId entityId, World world, EntityManager entityManager)
            {
                switch (authority)
                {
                    case Authority.Authoritative:
                        if (!entityManager.HasComponent<NotAuthoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity))
                        {
                            LogInvalidAuthorityTransition(Authority.Authoritative, Authority.NotAuthoritative, entityId);
                            return;
                        }

                        entityManager.RemoveComponent<NotAuthoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity);
                        entityManager.AddComponent(entity, ComponentType.Create<Authoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>());

                        break;
                    case Authority.AuthorityLossImminent:
                        if (!entityManager.HasComponent<Authoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity))
                        {
                            LogInvalidAuthorityTransition(Authority.AuthorityLossImminent, Authority.Authoritative, entityId);
                            return;
                        }

                        entityManager.AddComponent(entity, ComponentType.Create<AuthorityLossImminent<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>());
                        break;
                    case Authority.NotAuthoritative:
                        if (!entityManager.HasComponent<Authoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity))
                        {
                            LogInvalidAuthorityTransition(Authority.NotAuthoritative, Authority.Authoritative, entityId);
                            return;
                        }

                        if (entityManager.HasComponent<AuthorityLossImminent<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity))
                        {
                            entityManager.RemoveComponent<AuthorityLossImminent<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity);
                        }

                        entityManager.RemoveComponent<Authoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>(entity);
                        entityManager.AddComponent(entity, ComponentType.Create<NotAuthoritative<global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents.Component>>());
                        break;
                }
            }

            // todo delete this or fix it
            private void LogInvalidAuthorityTransition(Authority newAuthority, Authority expectedOldAuthority, global::Improbable.Gdk.Core.EntityId entityId)
            {
                // LogDispatcher.HandleLog(LogType.Error, new LogEvent(InvalidAuthorityChange)
                //     .WithField(LoggingUtils.LoggerName, LoggerName)
                //     .WithField(LoggingUtils.EntityId, entityId.Id)
                //     .WithField("New Authority", newAuthority)
                //     .WithField("Expected Old Authority", expectedOldAuthority)
                //     .WithField("Component", "global::Improbable.Gdk.Tests.ComponentsWithNoFields.ComponentWithNoFieldsWithEvents")
                // );
            }
        }
    }
}
#endif
