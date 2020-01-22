using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitPhysicsSystem : SpatialComponentSystem
    {
        struct PhysInfo
        {
            public bool isGrounded { get; private set;}
            public bool isBuilding { get; private set;}
            public bool isNotAlive { get; private set;}

            public PhysInfo(bool isGrounded, bool isBuilding, bool isNotAlive)
            {
                this.isGrounded = isGrounded;
                this.isBuilding = isBuilding;
                this.isNotAlive = isNotAlive;
            }

            public bool CheckChanged(bool isGrounded, bool isNotAlive)
            {
                bool isChanged = false;
                IsChanged |= this.isGrounded != isGrounded;
                IsChanged |= this.isNotAlive != isNotAlive;

                this.isGrounded = isGrounded;
                this.isNotAlive = isNotAlive;
                return IsChanged;
            } 
        }

        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        Ray vertical = new Ray();
        //readonly int layer = //LayerMask.//LayerMask.GetMask("Ground");
        readonly Dicitonary<EntityId,PhysInfo> physDic = new Dicitonary<EntityId, PhysInfo>();
        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
                if (unit == null)
                    return;

                var isGrounded = unit.GetGrounded();
                var isBuilding = UnitPhysicsDictionary.IsBuilding(status.Type);
                var isNotAlive = status.State != UnitState.Alive;

                var id = entityId.EntityId;
                if (physDic.ContainsKey(id) == false) {
                    physDic.Add(id, new PhysInfo(isGrounded, isBuilding, isNotAlive));
                }
                else if (physDic[id].CheckChanged(isGrounded, isNotAlive) == false) {
                    return; 
                }

                var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);
                rigidbody.freezeRotation = isGrounded & (isBuilding | !inNotAlive);
                if (isGrounded & isBuilding)
                    rigidbody.constraints = RigidBodyConstraints.FreezePosition;
            });
        }
    }
}
