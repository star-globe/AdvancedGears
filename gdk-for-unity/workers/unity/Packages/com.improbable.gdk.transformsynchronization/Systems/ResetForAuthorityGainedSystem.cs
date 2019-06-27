﻿using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using UnityEngine;

namespace Improbable.Gdk.TransformSynchronization
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(DefaultUpdateLatestTransformSystem))]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    public class ResetForAuthorityGainedSystem : ComponentSystem
    {
        private WorkerSystem worker;
        private ComponentUpdateSystem updateSystem;
        private EntityQuery rigidbodyGroup;
        private EntityQuery transformGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            worker = World.GetExistingSystem<WorkerSystem>();
            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            rigidbodyGroup = GetEntityQuery(
                ComponentType.ReadOnly<Rigidbody>(),
                ComponentType.ReadOnly<TransformInternal.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<TicksSinceLastTransformUpdate>(),
                ComponentType.ReadWrite<BufferedTransform>(),
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                ComponentType.ReadOnly<TransformInternal.ComponentAuthority>()
            );
            rigidbodyGroup.SetFilter(TransformInternal.ComponentAuthority.Authoritative);

            transformGroup = GetEntityQuery(
                ComponentType.ReadOnly<UnityEngine.Transform>(),
                ComponentType.ReadOnly<TransformInternal.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>(),
                ComponentType.ReadWrite<TicksSinceLastTransformUpdate>(),
                ComponentType.ReadWrite<BufferedTransform>(),
                ComponentType.Exclude<NewlyAddedSpatialOSEntity>(),
                ComponentType.Exclude<Rigidbody>(),
                ComponentType.ReadOnly<TransformInternal.ComponentAuthority>()
            );
            transformGroup.SetFilter(TransformInternal.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            UpdateRigidbodyData();
            UpdateTransformData();
        }

        private void UpdateRigidbodyData()
        {
            Entities.With(rigidbodyGroup).ForEach(
                (Entity entity, DynamicBuffer<BufferedTransform> buffer,
                    ref TicksSinceLastTransformUpdate ticksSinceLastTransformUpdate,
                    ref TransformInternal.Component transformInternal,
                    ref SpatialEntityId spatialEntityId) =>
                {
                    // todo this is not a correct constraint. Needs a the auth loss temporary exposed to correctly do this
                    // alternatively this needs an authority changed component that is filled at the beginning of the tick
                    if (updateSystem
                        .GetAuthorityChangesReceived(spatialEntityId.EntityId, TransformInternal.ComponentId)
                        .Count == 0)
                    {
                        return;
                    }

                    var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);
                    rigidbody.MovePosition(transformInternal.Location.ToUnityVector3() + worker.Origin);
                    rigidbody.MoveRotation(transformInternal.Rotation.ToUnityQuaternion());
                    rigidbody.AddForce(transformInternal.Velocity.ToUnityVector3() - rigidbody.velocity,
                        ForceMode.VelocityChange);

                    buffer.Clear();
                    ticksSinceLastTransformUpdate = new TicksSinceLastTransformUpdate();
                });
        }

        private void UpdateTransformData()
        {
            Entities.With(transformGroup).ForEach((Entity entity,
                DynamicBuffer<BufferedTransform> buffer,
                ref TicksSinceLastTransformUpdate ticksSinceLastTransformUpdate,
                ref TransformInternal.Component transformInternal,
                ref SpatialEntityId spatialEntityId) =>
            {
                if (updateSystem
                    .GetAuthorityChangesReceived(spatialEntityId.EntityId, TransformInternal.ComponentId)
                    .Count == 0)
                {
                    return;
                }

                var unityTransform = EntityManager.GetComponentObject<UnityEngine.Transform>(entity);
                unityTransform.position = transformInternal.Location.ToUnityVector3() + worker.Origin;
                unityTransform.rotation = transformInternal.Rotation.ToUnityQuaternion();

                buffer.Clear();
                ticksSinceLastTransformUpdate = new TicksSinceLastTransformUpdate();
            });
        }
    }
}
