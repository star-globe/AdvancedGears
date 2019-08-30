using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    [UpdateAfter(typeof(AdvancedInputSync))]
    internal class MoveAdvancedUnitSystem : BaseEntitySearchSystem
    {
        public struct Speed : IComponentData
        {
            public float CurrentSpeed;
            public float SpeedSmoothVelocity;
        }

        private EntityQuery newAdvancedGroup;
        private EntityQuery advancedInputGroup;

        private const float WalkSpeed = 2.0f;
        private const float RunSpeed = 6.0f;
        private const float MaxSpeed = 8.0f;

        private const float TurnSmoothTime = 0.2f;
        private float turnSmoothVelocity;

        private const float SpeedSmoothTime = 0.1f;

        protected override void OnCreate()
        {
            base.OnCreate();

            newAdvancedGroup = GetEntityQuery(
                ComponentType.ReadOnly<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<AdvancedUnitController.ComponentAuthority>(),
                ComponentType.Exclude<Speed>()
            );
            newAdvancedGroup.SetFilter(AdvancedUnitController.ComponentAuthority.Authoritative);

            advancedInputGroup = GetEntityQuery(
                ComponentType.ReadWrite<Rigidbody>(),
                ComponentType.ReadWrite<Speed>(),
                ComponentType.ReadOnly<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<TransformInternal.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );
            advancedInputGroup.SetFilter(TransformInternal.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleNews();
            HandleControllers();
            HandleEvets();
        }

        private void HandleNews()
        {
            using (var newSpeedEntities = newAdvancedGroup.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in newSpeedEntities)
                {
                    var speed = new Speed
                    {
                        CurrentSpeed = 0f,
                        SpeedSmoothVelocity = 0f
                    };

                    PostUpdateCommands.AddComponent(entity, speed);
                }
            }
        }

        private void HandleControllers()
        {
            Entities.With(advancedInputGroup).ForEach((Entity entity,
                                                       ref AdvancedUnitController.Component unitController,
                                                       ref BaseUnitStatus.Component status,
                                                       ref Speed speed) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                // todo Fuel check

                var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);
                var contoroller = unitController.Controller;
                var inputDir = new Vector2(contoroller.Horizontal, contoroller.Vertical).normalized;
                if (inputDir != Vector2.zero) {
                    var targetRotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg;
                    rigidbody.transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(
                        rigidbody.transform.eulerAngles.y, targetRotation,
                        ref turnSmoothVelocity, TurnSmoothTime);
                }
                var targetSpeed = (playerInput.Running ? RunSpeed : WalkSpeed) * inputDir.magnitude;
                var currentSpeed = speed.CurrentSpeed;
                var speedSmoothVelocity = speed.SpeedSmoothVelocity;
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, SpeedSmoothTime, MaxSpeed, Time.deltaTime);

                speed = new Speed
                {
                    CurrentSpeed = currentSpeed,
                    SpeedSmoothVelocity = speedSmoothVelocity
                };
                // This needs to be used instead of add force because this is running in update.
                // It would be better to store this in another component and have something else use it on fixed update.
                rigidbody.velocity = rigidbody.transform.forward * currentSpeed;
            });
        }

        private void HandleEvets()
        {
            var controllerEvents = UpdateSystem.GetEventsReceived<AdvancedUnitController.ControllerChanged.Event>();
            for (var i = 0; i < controllerEvents.Count; i++)
            {
                var ctrlEvent = controllerEvents[i];
                AdvancedUnitController.Component? comp = null;
                if (TryGetComponent(ctrlEvent.EntityId, out comp)) {
                    var value = comp.Value;
                    value.Controller = ctrlEvent.Payload;
                    SetComponent(ctrlEvent.EntityId, value);
                }
            }
        }
    }
}
