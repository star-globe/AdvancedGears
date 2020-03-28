using System.Collections.Generic;
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
                isChanged |= this.isGrounded != isGrounded;
                isChanged |= this.isNotAlive != isNotAlive;

                this.isGrounded = isGrounded;
                this.isNotAlive = isNotAlive;
                return isChanged;
            } 
        }

        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<Rigidbody>(),
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );
        }

        //Ray vertical = new Ray();
        //readonly int layer = //LayerMask.//LayerMask.GetMask("Ground");
        //readonly Dictionary<EntityId,PhysInfo> physDic = new Dictionary<EntityId, PhysInfo>();
        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity, ref BaseUnitStatus.Component status) =>
            {
                var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);
                if (rigidbody == null ||
                    rigidbody.isKinematic)
                    return;

                var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
                if (unit == null)
                    return;

                if (unit.GetGrounded() && UnitPhysicsDictionary.IsBuilding(status.Type))
                    rigidbody.isKinematic = true;
            });
        }
    }
}
