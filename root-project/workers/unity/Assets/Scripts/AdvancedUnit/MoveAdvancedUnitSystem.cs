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
            public float CurrentRotSpeed;
            public float SpeedSmoothRotVelocity;
        }

        private EntityQuery newAdvancedGroup;
        private EntityQuery advancedInputGroup;
        private EntityQueryBuilder.F_ECDDD<Rigidbody, LocalController, BaseUnitStatus.Component, Speed> action;

        private const float WalkSpeed = 10.0f;
        private const float RunSpeed = 3.0f * WalkSpeed;
        private const float MaxSpeed = 1.2f * RunSpeed;

        private const float TurnSpeed = 7.0f;
        private const float MaxTurnSpeed = 1.2f * TurnSpeed;

        private const float InverseSpeedRate = 15.0f;

        private const float TurnSmoothTime = 0.05f;
        private float turnSmoothVelocity;

        private const float SpeedSmoothTime = 0.05f;

        protected override void OnCreate()
        {
            base.OnCreate();

            newAdvancedGroup = GetEntityQuery(
                ComponentType.ReadOnly<LocalController>(),
                ComponentType.Exclude<Speed>()
            );

            advancedInputGroup = GetEntityQuery(
                ComponentType.ReadWrite<Rigidbody>(),
                ComponentType.ReadWrite<Speed>(),
                ComponentType.ReadOnly<UnitTransform>(),
                ComponentType.ReadOnly<LocalController>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>()
            );

            action = Query;
       }

        protected override void OnUpdate()
        {
            HandleNews();
            HandleControllers();
            //HandleEvets();
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
                        SpeedSmoothVelocity = 0f,
                        CurrentRotSpeed = 0f,
                        SpeedSmoothRotVelocity = 0f,
                    };

                    PostUpdateCommands.AddComponent(entity, speed);
                }
            }
        }

        private void HandleControllers()
        {
            Entities.With(advancedInputGroup).ForEach(action);
        }    
        
        private void Query(Entity entity,
                            Rigidbody rigidbody,
                            ref LocalController localController,
                            ref BaseUnitStatus.Component status,
                            ref Speed speed)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Type != UnitType.Advanced)
                return;

            // todo Fuel check

            var stick = localController.Stick;
            var inputDir = new Vector2(stick.Horizontal, stick.Vertical).normalized;
            var inputCam = new Vector2(stick.Yaw, stick.Pitch);

            var currentSpeed = speed.CurrentSpeed;
            var speedSmoothVelocity = speed.SpeedSmoothVelocity;
            var currentRotSpeed = speed.CurrentRotSpeed;
            var speedSmoothRotVelocity = speed.SpeedSmoothRotVelocity;

            bool updateSpeed = false;
            var x = rigidbody.velocity.x;
            var z = rigidbody.velocity.z;
            if (x * x + z * z <= MaxSpeed * MaxSpeed)
            {
                var targetSpeed = (localController.Action.Running ? RunSpeed : WalkSpeed) * inputDir.magnitude;
                currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, SpeedSmoothTime, MaxSpeed, Time.DeltaTime);

                updateSpeed = true;
            }

            Vector3? rotVector = null;
            var anglerVelocity = rigidbody.angularVelocity;
            if (anglerVelocity.y < MaxTurnSpeed && anglerVelocity.y > -MaxTurnSpeed)
            {
                float rate = inputCam.x * anglerVelocity.y < 0 ? InverseSpeedRate : 1.0f;

                var targetSpeed = rate * inputCam.x * TurnSpeed;
                currentRotSpeed = Mathf.SmoothDamp(currentRotSpeed, targetSpeed, ref speedSmoothRotVelocity, SpeedSmoothTime, MaxTurnSpeed, Time.DeltaTime);

                rotVector = Vector2.up * currentRotSpeed;
            }

            speed = new Speed
            {
                CurrentSpeed = currentSpeed,
                SpeedSmoothVelocity = speedSmoothVelocity,
                CurrentRotSpeed = currentRotSpeed,
                SpeedSmoothRotVelocity = speedSmoothRotVelocity,
            };
            // This needs to be used instead of add force because this is running in update.
            // It would be better to store this in another component and have something else use it on fixed update.

            if (rotVector != null)
            {
                rigidbody.AddTorque(rotVector.Value, ForceMode.Acceleration);
            }

            if (updateSpeed)
            {
                var vec = new Vector3(inputDir.x, 0, inputDir.y);
                vec = rigidbody.transform.TransformDirection(vec);
                rigidbody.AddForce(vec * currentSpeed, ForceMode.Acceleration);
            }
        }

        //private void HandleEvets()
        //{
        //    var controllerEvents = UpdateSystem.GetEventsReceived<AdvancedUnitController.ControllerChanged.Event>();
        //    for (var i = 0; i < controllerEvents.Count; i++)
        //    {
        //        var ctrlEvent = controllerEvents[i];
        //        AdvancedUnitController.Component? comp = null;
        //        if (TryGetComponent(ctrlEvent.EntityId, out comp)) {
        //            var value = comp.Value;
        //            value.Controller = ctrlEvent.Event.Payload;
        //            SetComponent(ctrlEvent.EntityId, value);
        //        }
        //    }
        //}
    }
}
