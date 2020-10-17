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
    public class SyncTransObjectCreation : IEntityGameObjectCreator
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

        private readonly Type[] componentsToAddForAnim =
        {
            typeof(Transform), typeof(Rigidbody), typeof(PostureBoneContainer)
        };

        private readonly Dictionary<EntityId, GameObject> gameObjectsCreated = new Dictionary<EntityId, GameObject>();

        public SyncTransObjectCreation(WorkerInWorld worker)
        {
            this.worker = worker;
        }

        public void OnEntityCreated(SpatialOSEntityInfo entityInfo, GameObject prefab, EntityManager entityManager, EntityGameObjectLinker linker)
        {
            Coordinates position = Coordinates.Zero;
            if (TryGetComponent<Position.Component>(entityManager, entityInfo.Entity, out var pos))
                position = pos.Value.Coords;

            Quaternion rot = Quaternion.identity;
            Vector3 scale = Vector3.one;
            if (TryGetComponent<PostureRoot.Component>(entityManager, entityInfo.Entity, out var posture)) {
                rot = posture.Value.RootTrans.Rotation.ToUnityQuaternion();
                scale = posture.Value.RootTrans.Scale.ToUnityVector();
            }

            Dictionary<int,CompressedLocalTransform> boneMap = null;
            if (TryGetComponent<PostureAnimation.Component>(entityManager, entityInfo.Entity, out var anim))
                boneMap = anim.Value.BoneMap;

            var gameObject = UnityEngine.Object.Instantiate(prefab, position.ToUnityVector() + this.WorkerOrigin, rot);
            gameObject.transform.localScale = scale;

            Type[] types = componentsToAdd;
            if (boneMap != null) {
                var container = gameObject.GetComponent<PostureBoneContainer>();
                container?.SetTrans(boneMap);
                types = componentsToAddForAnim;
            }

            gameObjectsCreated.Add(entityInfo.SpatialOSEntityId, gameObject);
            gameObject.name = $"{prefab.name}(SpatialOS: {entityInfo.SpatialOSEntityId}, Worker: {this.WorkerType})";
            linker.LinkGameObjectToSpatialOSEntity(entityInfo.SpatialOSEntityId, gameObject, types);
        }

        private bool TryGetComponent<T>(EntityManager entityManager, in Entity entity, out T? comp) where T : struct, IComponentData
        {
            comp = null;
            if (entityManager == null)
                return false;

            if (entityManager.HasComponent<T>(entity))
            {
                comp = entityManager.GetComponentData<T>(entity);
                return true;
            }
            else
                return false;
        }

        public void OnEntityRemoved(EntityId entityId)
        {
            if (!gameObjectsCreated.TryGetValue(entityId, out var gameObject))
            {
                return;
            }

            gameObjectsCreated.Remove(entityId);
            UnityEngine.Object.Destroy(gameObject);
        }

        public void PopulateEntityTypeExpectations(EntityTypeExpectations entityTypeExpectations)
        {
        }
    }
}
