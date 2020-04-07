using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class MoveAdvancedUnitSystem : BaseEntitySearchSystem
    {
        public struct Speed : IComponentData
        {
            public float CurrentSpeed;
            public float SpeedSmoothVelocity;
        }

        private EntityQuery newAdvancedGroup;
        private EntityQuery advancedInputGroup;

        private const float WalkSpeed = 16.0f;
        private const float RunSpeed = 20.0f;
        private const float MaxSpeed = 25.0f;

        private const float TurnSpeed = 4.0f;

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
                ComponentType.ReadOnly<UnitTransform>(),
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

                if (status.Type != UnitType.Advanced)
                    return;

                // todo Fuel check

                var rigidbody = EntityManager.GetComponentObject<Rigidbody>(entity);
                var controller = unitController.Controller;
                var inputDir = new Vector2(controller.Horizontal, controller.Vertical).normalized;

                var trans = rigidbody.transform;
                if (inputDir.x * inputDir.x > 0.0f) {
                    //Vector3 up = trans.up;
                    //var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
                    //if (unit != null && unit.GetGrounded(out var hit))
                    //{
                    //    up = hit.normal;
                    //    var fwd = new Vector3(trans.forward.x, 0, trans.forward.z);
                    //    trans.rotation = Quaternion.LookRotation(fwd.normalized, hit.normal);
                    //}

                    trans.Rotate(Vector3.up, inputDir.x * TurnSpeed);
                    var fwd = new Vector3(trans.forward.x, 0, trans.forward.z);
                    trans.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                }

                var x = rigidbody.velocity.x;
                var z = rigidbody.velocity.z;
                if (x * x + z * z > MaxSpeed * MaxSpeed)
                    return;

                var targetSpeed = (unitController.Controller.Running ? RunSpeed : WalkSpeed) * inputDir.y;
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

                rigidbody.AddForce(rigidbody.transform.forward * currentSpeed, ForceMode.Acceleration);


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
                    value.Controller = ctrlEvent.Event.Payload;
                    SetComponent(ctrlEvent.EntityId, value);
                }
            }
        }
    }
}
