using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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
        IntervalChecker inter;
        EntityQueryBuilder.F_DCC<BaseUnitStatus.Component, Rigidbody, UnitTransform> action;
        const int period = 10;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<Rigidbody>(),
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<BuildingData>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(period);
            action = Query;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach(action);
        }

        private void Query(ref BaseUnitStatus.Component status, Rigidbody rigidbody, UnitTransform unit)
        {
#if true
            if (UnitUtils.IsBuilding(status.Type) == false)
                return;

            if (rigidbody == null ||
                rigidbody.isKinematic)
                return;

            if (unit != null && unit.GetGrounded())
                rigidbody.isKinematic = true;
#endif
        }
    }

    public struct BuildingData : IComponentData
    {
        public static BuildingData CreateData()
        {
            return new BuildingData();
        }
    }
}
