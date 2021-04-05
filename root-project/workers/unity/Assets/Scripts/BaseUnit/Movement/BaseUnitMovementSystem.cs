using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitMovementSystem : SpatialComponentSystem
    {
        EntityQuery group;
        EntityQueryBuilder.F_EDD<BaseUnitMovement.Component, BaseUnitStatus.Component> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach(action);
        }

        private void Query(Entity entity,
                                          ref BaseUnitMovement.Component movement,
                                          ref BaseUnitStatus.Component status)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Type != UnitType.Soldier &&
                status.Type != UnitType.Commander)
                return;

            var unit = EntityManager.GetComponentObject<UnitTransform>(entity);

            // check ground
            if (unit == null || unit.GetGrounded(out var hitInfo) == false)
                return;

            var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);

            if (movement.MoveSpeed == 0.0f &&
                movement.RotSpeed == 0.0f)
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
        }
    }
}
