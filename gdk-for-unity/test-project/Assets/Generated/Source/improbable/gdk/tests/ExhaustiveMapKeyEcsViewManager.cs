// ===========
// DO NOT EDIT - this file is automatically regenerated.
// ===========

using System;
using Unity.Entities;
using Improbable.Worker.CInterop;
using Improbable.Gdk.Core;

namespace Improbable.Gdk.Tests
{
    public partial class ExhaustiveMapKey
    {
        public class EcsViewManager : IEcsViewManager
        {
            private WorkerSystem workerSystem;
            private EntityManager entityManager;
            private World world;

            private readonly ComponentType[] initialComponents = new ComponentType[]
            {
                ComponentType.ReadWrite<Component>(),
                ComponentType.ReadWrite<ComponentAuthority>(),
            };

            public uint GetComponentId()
            {
                return ComponentId;
            }

            public ComponentType[] GetInitialComponents()
            {
                return initialComponents;
            }

            public void ApplyDiff(ViewDiff diff)
            {
                var diffStorage = (DiffComponentStorage) diff.GetComponentDiffStorage(ComponentId);

                foreach (var entityId in diffStorage.GetComponentsAdded())
                {
                    AddComponent(entityId);
                }

                var updates = diffStorage.GetUpdates();
                var dataFromEntity = workerSystem.GetComponentDataFromEntity<Component>();
                for (int i = 0; i < updates.Count; ++i)
                {
                    ApplyUpdate(in updates[i], dataFromEntity);
                }

                var authChanges = diffStorage.GetAuthorityChanges();
                for (int i = 0; i < authChanges.Count; ++i)
                {
                    ref readonly var change = ref authChanges[i];
                    SetAuthority(change.EntityId, change.Authority);
                }

                foreach (var entityId in diffStorage.GetComponentsRemoved())
                {
                    RemoveComponent(entityId);
                }
            }

            public void Init(World world)
            {
                this.world = world;
                entityManager = world.EntityManager;

                workerSystem = world.GetExistingSystem<WorkerSystem>();

                if (workerSystem == null)
                {
                    throw new ArgumentException("World instance is not running a valid SpatialOS worker");
                }
            }

            public void Clean(World world)
            {
                ExhaustiveMapKey.ReferenceTypeProviders.Field1Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field2Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field3Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field4Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field5Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field6Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field7Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field8Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field9Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field10Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field11Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field12Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field13Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field14Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field15Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field16Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field17Provider.CleanDataInWorld(world);
                ExhaustiveMapKey.ReferenceTypeProviders.Field18Provider.CleanDataInWorld(world);
            }

            private void AddComponent(EntityId entityId)
            {
                workerSystem.TryGetEntity(entityId, out var entity);

                var component = new global::Improbable.Gdk.Tests.ExhaustiveMapKey.Component();

                component.field1Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field1Provider.Allocate(world);
                component.field2Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field2Provider.Allocate(world);
                component.field3Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field3Provider.Allocate(world);
                component.field4Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field4Provider.Allocate(world);
                component.field5Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field5Provider.Allocate(world);
                component.field6Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field6Provider.Allocate(world);
                component.field7Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field7Provider.Allocate(world);
                component.field8Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field8Provider.Allocate(world);
                component.field9Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field9Provider.Allocate(world);
                component.field10Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field10Provider.Allocate(world);
                component.field11Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field11Provider.Allocate(world);
                component.field12Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field12Provider.Allocate(world);
                component.field13Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field13Provider.Allocate(world);
                component.field14Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field14Provider.Allocate(world);
                component.field15Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field15Provider.Allocate(world);
                component.field16Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field16Provider.Allocate(world);
                component.field17Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field17Provider.Allocate(world);
                component.field18Handle = Improbable.Gdk.Tests.ExhaustiveMapKey.ReferenceTypeProviders.Field18Provider.Allocate(world);
                component.MarkDataClean();
                entityManager.AddSharedComponentData(entity, ComponentAuthority.NotAuthoritative);
                entityManager.AddComponentData(entity, component);
            }

            private void RemoveComponent(EntityId entityId)
            {
                workerSystem.TryGetEntity(entityId, out var entity);
                entityManager.RemoveComponent<ComponentAuthority>(entity);

                var data = entityManager.GetComponentData<global::Improbable.Gdk.Tests.ExhaustiveMapKey.Component>(entity);
                ExhaustiveMapKey.ReferenceTypeProviders.Field1Provider.Free(data.field1Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field2Provider.Free(data.field2Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field3Provider.Free(data.field3Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field4Provider.Free(data.field4Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field5Provider.Free(data.field5Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field6Provider.Free(data.field6Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field7Provider.Free(data.field7Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field8Provider.Free(data.field8Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field9Provider.Free(data.field9Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field10Provider.Free(data.field10Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field11Provider.Free(data.field11Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field12Provider.Free(data.field12Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field13Provider.Free(data.field13Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field14Provider.Free(data.field14Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field15Provider.Free(data.field15Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field16Provider.Free(data.field16Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field17Provider.Free(data.field17Handle);
                ExhaustiveMapKey.ReferenceTypeProviders.Field18Provider.Free(data.field18Handle);

                entityManager.RemoveComponent<global::Improbable.Gdk.Tests.ExhaustiveMapKey.Component>(entity);
            }

            private void ApplyUpdate(in ComponentUpdateReceived<Update> update, ComponentDataFromEntity<Component> dataFromEntity)
            {
                workerSystem.TryGetEntity(update.EntityId, out var entity);
                if (!dataFromEntity.Exists(entity))
                {
                    return;
                }

                var data = dataFromEntity[entity];

                if (update.Update.Field1.HasValue)
                {
                    data.Field1 = update.Update.Field1.Value;
                }

                if (update.Update.Field2.HasValue)
                {
                    data.Field2 = update.Update.Field2.Value;
                }

                if (update.Update.Field3.HasValue)
                {
                    data.Field3 = update.Update.Field3.Value;
                }

                if (update.Update.Field4.HasValue)
                {
                    data.Field4 = update.Update.Field4.Value;
                }

                if (update.Update.Field5.HasValue)
                {
                    data.Field5 = update.Update.Field5.Value;
                }

                if (update.Update.Field6.HasValue)
                {
                    data.Field6 = update.Update.Field6.Value;
                }

                if (update.Update.Field7.HasValue)
                {
                    data.Field7 = update.Update.Field7.Value;
                }

                if (update.Update.Field8.HasValue)
                {
                    data.Field8 = update.Update.Field8.Value;
                }

                if (update.Update.Field9.HasValue)
                {
                    data.Field9 = update.Update.Field9.Value;
                }

                if (update.Update.Field10.HasValue)
                {
                    data.Field10 = update.Update.Field10.Value;
                }

                if (update.Update.Field11.HasValue)
                {
                    data.Field11 = update.Update.Field11.Value;
                }

                if (update.Update.Field12.HasValue)
                {
                    data.Field12 = update.Update.Field12.Value;
                }

                if (update.Update.Field13.HasValue)
                {
                    data.Field13 = update.Update.Field13.Value;
                }

                if (update.Update.Field14.HasValue)
                {
                    data.Field14 = update.Update.Field14.Value;
                }

                if (update.Update.Field15.HasValue)
                {
                    data.Field15 = update.Update.Field15.Value;
                }

                if (update.Update.Field16.HasValue)
                {
                    data.Field16 = update.Update.Field16.Value;
                }

                if (update.Update.Field17.HasValue)
                {
                    data.Field17 = update.Update.Field17.Value;
                }

                if (update.Update.Field18.HasValue)
                {
                    data.Field18 = update.Update.Field18.Value;
                }

                data.MarkDataClean();
                dataFromEntity[entity] = data;
            }

            private void SetAuthority(EntityId entityId, Authority authority)
            {
                switch (authority)
                {
                    case Authority.NotAuthoritative:
                    {
                        workerSystem.TryGetEntity(entityId, out var entity);
                        entityManager.SetSharedComponentData(entity, ComponentAuthority.NotAuthoritative);
                        break;
                    }
                    case Authority.Authoritative:
                    {
                        workerSystem.TryGetEntity(entityId, out var entity);
                        entityManager.SetSharedComponentData(entity, ComponentAuthority.Authoritative);
                        break;
                    }
                    case Authority.AuthorityLossImminent:
                        break;
                }
            }
        }
    }
}
