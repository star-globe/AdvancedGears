using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Representation;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class SyncRootObjectCreation : IEntityGameObjectCreator
    {
        WorkerInWorld worker = null;

        Vector3 WorkerOrigin
        {
            get
            {
                if (worker == null)
                    return Vector3.zero;

                return worker.Origin;
            }
        }

        string WorkerType
        {
            get
            {
                if (worker == null)
                    return string.Empty;

                return worker.WorkerType;
            }
        }

        private readonly Type[] componentsToAdd =
        {
            typeof(Transform), typeof(Rigidbody)
        };

        public SyncRootObjectCreation(WorkerInWorld worker)
        {
            this.worker = worker;
        }

        public void OnEntityCreated(SpatialOSEntityInfo entityInfo, GameObject prefab, EntityManager entityManager, EntityGameObjectLinker linker)
        {
            Coordinates position = Coordinates.Zero;
            if (entityManager.HasComponent<Position.Component>(entityInfo.Entity))
            {
                var pos = entityManager.GetComponentData<Position.Component>(entityInfo.Entity);
                position = pos.Coords;
            }

            Quaternion rot = Quaternion.identity;
            Vector3 scale = Vector3.one;
            if (entityManager.HasComponent<PostureRoot.Component>(entityInfo.Entity)) {
                var root = entityManager.GetComponentData<PostureRoot.Component>(entityInfo.Entity);
                rot = root.RootTrans.Rotation.ToUnityQuaternion();
                scale = root.RootTrans.Scale.ToUnityVector();
            }

            var gameObject = UnityEngine.Object.Instantiate(prefab, position.ToUnityVector() + this.WorkerOrigin, rot);
            gameObject.transform.localScale = scale;

            gameObject.name = $"{prefab.name}(SpatialOS: {entityInfo.SpatialOSEntityId}, Worker: {this.WorkerType})";
            linker.LinkGameObjectToSpatialOSEntity(entityInfo.SpatialOSEntityId, gameObject, componentsToAdd);
        }

        public void OnEntityRemoved(EntityId entityId)
        {
        }

        public void PopulateEntityTypeExpectations(EntityTypeExpectations entityTypeExpectations)
        {
        }
    }
}
