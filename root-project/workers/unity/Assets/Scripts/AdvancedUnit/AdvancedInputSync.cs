using System;
using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class AdvancedPlayerInputSync : AdvancedInputSync
    {
        private EntityQuery inputPlayerGroup;

        protected override void OnCreate()
        {
            // local
            inputPlayerGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedPlayerInput.Component>(),
                ComponentType.ReadOnly<CameraTransform>(),
                ComponentType.ReadOnly<AdvancedPlayerInput.HasAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()

            );
        }

        protected override void OnUpdate()
        {
            Entities.With(inputPlayerGroup).ForEach((ref CameraTransform cameraTransform,
                                                     ref AdvancedPlayerInput.Component playerInput,
                                                     ref SpatialEntityId entityId) =>
            {
                var input = InputUtils.GetMove();
                var inputCam = InputUtils.GetCamera();
                var isShiftDown = Input.GetKey(KeyCode.LeftShift);
                var isJump = Input.GetKey(KeyCode.Space);
                var controller = playerInput.LocalController;
                CommonUpdate(input, inputCam, isShiftDown, isJump, entityId, ref controller);
                playerInput.LocalController = controller;
            });
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class AdvancedUnmannedInputSync : AdvancedInputSync
    {
        private EntityQuery inputUnmannedGroup;

        protected override void OnCreate()
        {
            // unmanned
            inputUnmannedGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedUnmannedInput.Component>(),
                ComponentType.ReadOnly<AdvancedUnmannedInput.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(inputUnmannedGroup).ForEach((ref BaseUnitStatus.Component status,
                                                       ref AdvancedUnmannedInput.Component unMannedInput,
                                                       ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var inter = unMannedInput.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                unMannedInput.Interval = inter;
                var x = UnityEngine.Random.Range(-1.0f, 1.0f);
                var y = UnityEngine.Random.Range(-1.0f, 1.0f);
                var isShiftDown = Input.GetKey(KeyCode.LeftShift);
                var isJump = Input.GetKey(KeyCode.Space);
                var controller = unMannedInput.LocalController;
                CommonUpdate(new Vector2(x, y), new Vector2(x, y), isShiftDown, isJump, entityId, ref controller);
                unMannedInput.LocalController = controller;
            });
        }
    }

    internal abstract class AdvancedInputSync : SpatialComponentSystem
    {
        private const float MinInputChange = 0.01f;
        private const float MinInputCamera = 0.01f;

        protected void CommonUpdate(in Vector2 inputPos, in Vector3 inputCam, bool isShiftDown, bool isJump, in SpatialEntityId entityId, ref ControllerInfo oldController)
        {
            if (CheckChange(oldController.Horizontal, inputPos.x) ||
                CheckChange(oldController.Vertical, inputPos.y) ||
                CheckChange(oldController.Yaw, inputCam.x) ||
                CheckChange(oldController.Pitch, inputCam.y) ||
                oldController.Running != isShiftDown)
            {
                var newController = new ControllerInfo
                {
                    Horizontal = inputPos.x,
                    Vertical = inputPos.y,
                    Yaw = inputCam.x,
                    Pitch = inputCam.y,
                    Running = isShiftDown,
                    Jump = isJump
                };
                oldController = newController;
                UpdateSystem.SendEvent(new AdvancedUnitController.ControllerChanged.Event(newController), entityId.EntityId);
            }
        }

        private bool CheckChange(float oldValue, float newValue)
        {
            return Math.Abs(oldValue - newValue) > MinInputChange;
        }

        private float ClampCamera(float inputCam)
        {
            if (inputCam > -MinInputCamera &&
                inputCam < MinInputCamera)
                return 0;
        
            return Mathf.Clamp(inputCam, -1.0f, 1.0f);
        }
    }
}
