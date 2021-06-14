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
        EntityQueryBuilder.F_EDD<MovementData, BaseUnitStatus.Component> movementAction;

        EntityQuerySet syncQuerySet;
        EntityQueryBuilder.F_EDDD<MovementData, BaseUnitMovement.Component, BaseUnitStatus.Component> syncAction;
        const int syncInterval = 4;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadOnly<UnitTransform>(),
                    ComponentType.ReadOnly<MovementData>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );

            movementAction = MovementQuery;

            syncQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadOnly<MovementData>(),
                                                ComponentType.ReadWrite<BaseUnitMovement.Component>(),
                                                ComponentType.ReadOnly<BaseUnitMovement.HasAuthority>(),
                                                ComponentType.ReadOnly<BaseUnitStatus.Component>()), syncInterval);

            syncAction = SyncQuery;
        }

        protected override void OnUpdate()
        {
            UpdateMovement();
            SyncMovement();
        }

        private void UpdateMovement()
        {
            Entities.With(group).ForEach(movementAction);
        }

        private void SyncMovement()
        {
            if (CheckTime(ref syncQuerySet.inter) == false)
                return;

            Entities.With(syncQuerySet.group).ForEach(syncAction);
        }

        private void MovementQuery(Entity entity,
                                          ref MovementData movement,
                                          ref BaseUnitStatus.Component status)
        {
            if (status.State != UnitState.Alive)
                return;
            
            if (UnitUtils.IsAutomaticallyMoving(status.Type) == false)
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

        const float moveDiff = 0.01f;
        const float rotDiff = 0.01f;
        private void SyncQuery(Entity entity,
                                          ref MovementData movement,
                                          ref BaseUnitMovement.Component baseMovement,
                                          ref BaseUnitStatus.Component status)
        {
            if (status.State != UnitState.Alive)
                return;
            
            if (UnitUtils.IsAutomaticallyMoving(status.Type) == false)
                return;

            var m_diff = baseMovement.MoveSpeed - movement.MoveSpeed;
            var r_diff = baseMovement.RotSpeed - movement.RotSpeed;
            
            if (m_diff * m_diff < moveDiff * moveDiff &&
                r_diff * r_diff < rotDiff * rotDiff)
                return;

            baseMovement.MoveSpeed = movement.MoveSpeed;
            baseMovement.RotSpeed = movement.RotSpeed;
        }
    }

    public struct MovementData : IComponentData
    {
        public float MoveSpeed;
        public flot RotSpeed;

        public static MovementData CreateData(float move, float rot)
        {
            return new MovementData() { MoveSpeed = move, RotSpeed = rot, };
        }
    }
}
