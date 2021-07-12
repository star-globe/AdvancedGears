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

        bool? isClient = null;
        bool IsClient
        {
            get
            {
                if (isClient == null)
                {
                    isClient = false;
                    foreach (var w in WorkerUtils.AllClientAttributes)
                    {
                        if (string.Equals(this.WorkerType, w))
                        {
                            isClient = true;
                            break;
                        }
                    }
                }

                return isClient.Value;
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
            if (TryGetComponent<Position.Component>(ref entityManager, entityInfo.Entity, out var pos))
                position = pos.Value.Coords;

            Quaternion rot = Quaternion.identity;
            Vector3 scale = Vector3.one;
            if (TryGetComponent<PostureRoot.Component>(ref entityManager, entityInfo.Entity, out var posture)) {
                rot = posture.Value.RootTrans.Rotation.ToUnityQuaternion();
                scale = posture.Value.RootTrans.Scale.ToUnityVector();
            }

            Dictionary<int,CompressedLocalTransform> boneMap = null;
            if (TryGetComponent<BoneAnimation.Component>(ref entityManager, entityInfo.Entity, out var anim))
                boneMap = anim.Value.BoneMap;

            var gameObject = UnityEngine.Object.Instantiate(prefab, position.ToUnityVector() + this.WorkerOrigin, rot);
            gameObject.transform.localScale = scale;

            Type[] types = componentsToAdd;
            if (boneMap != null) {
                var container = gameObject.GetComponent<PostureBoneContainer>();
                container?.SetTrans(boneMap);
            }

            if (this.IsClient == false) {
                if (TryGetComponent<BaseUnitMovement.Component>(ref entityManager, entityInfo.Entity, out var movement)) {
                    entityManager.AddComponentData<NavPathData>(entityInfo.Entity, NavPathData.CreateData());
                    entityManager.AddComponentData<MovementData>(entityInfo.Entity, MovementData.CreateData(movement.Value.MoveSpeed, movement.Value.RotSpeed));
                }

                if (TryGetComponent<BaseUnitStatus.Component>(ref entityManager, entityInfo.Entity, out var status)) {
                    if (UnitUtils.IsBuilding(status.Value.Type))
                        entityManager.AddComponentData<BuildingData>(entityInfo.Entity, BuildingData.CreateData());
                }
            }

            gameObjectsCreated.Add(entityInfo.SpatialOSEntityId, gameObject);
            gameObject.name = $"{prefab.name}(SpatialOS: {entityInfo.SpatialOSEntityId}, Worker: {this.WorkerType})";
            linker.LinkGameObjectToSpatialOSEntity(entityInfo.SpatialOSEntityId, gameObject, types);
        }

        private bool TryGetComponent<T>(ref EntityManager entityManager, in Entity entity, out T? comp) where T : struct, IComponentData
        {
            comp = null;
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
