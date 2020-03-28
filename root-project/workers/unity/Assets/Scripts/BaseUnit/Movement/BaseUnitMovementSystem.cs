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
    internal class BaseUnitMovementSystem : SpatialComponentSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<BaseUnitPosture.Component>(),
                    ComponentType.ReadOnly<BaseUnitPosture.ComponentAuthority>(),
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadWrite<FuelComponent.Component>(),
                    ComponentType.ReadOnly<FuelComponent.ComponentAuthority>()
            );

            group.SetFilter(BaseUnitPosture.ComponentAuthority.Authoritative);
            group.SetFilter(FuelComponent.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitPosture.Component posture,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitStatus.Component status,
                                          ref FuelComponent.Component fuel) =>
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

                if (movement.RotSpeed != 0.0f) {
                    trans.Rotate(trans.up, movement.RotSpeed * Time.fixedDeltaTime);

                    var inter = posture.Interval;
                    if (posture.Initialized && inter.CheckTime())
                    {
                        posture.Interval = inter;
                        posture.Root = rigidbody.transform.rotation.ToCompressedQuaternion();
                    }
                }

                var consume = (int)(moveVec.magnitude * movement.ConsumeRate);
                fuel.Fuel -= consume;
                if (fuel.Fuel < 0)
                    fuel.Fuel = 0;
            });
        }
    }
}
