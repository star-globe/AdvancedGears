using System;
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
    internal class BaseUnitMovementSystem : SpatialComponentSystem
    {
        EntityQuery group;
        EntityQueryBuilder.F_EDDD<BaseUnitMovement.Component, BaseUnitStatus.Component, FuelComponent.Component> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadWrite<FuelComponent.Component>(),
                    ComponentType.ReadOnly<FuelComponent.HasAuthority>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach(action);
        }

        private void Query(Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitStatus.Component status,
                                          ref FuelComponent.Component fuel)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Type != UnitType.Soldier &&
                status.Type != UnitType.Commander)
                return;

            // check fueld
            if (fuel.Fuel == 0)
                return;

            var unit = EntityManager.GetComponentObject<UnitTransform>(entity);

            // check ground
            if (unit == null || unit.GetGrounded(out var hitInfo) == false)
                return;

            var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);

            if (movement.MoveSpeed == 0.0f &&
                movement.RotSpeed == 0.0f)//target.State == TargetState.None)
            {
                return;
            }

            var trans = rigidbody.transform;

            var uVec = trans.forward * movement.MoveSpeed;
            var moveVec = uVec * Time.fixedDeltaTime;

            //rigidbody.velocity = uVec;
            var pos = rigidbody.position;
            rigidbody.MovePosition(pos + moveVec);

            if (movement.RotSpeed != 0.0f)
                trans.Rotate(trans.up, movement.RotSpeed * Time.fixedDeltaTime);

            var consume = (int) (moveVec.magnitude * movement.ConsumeRate);
            fuel.Fuel -= consume;
            if (fuel.Fuel < 0)
                fuel.Fuel = 0;
        }
    }
}
