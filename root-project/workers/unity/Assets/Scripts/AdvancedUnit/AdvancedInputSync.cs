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
        private EntityQueryBuilder.F_DDDD<CameraTransform, AdvancedPlayerInput.Component, AdvancedUnitController.Component, SpatialEntityId> action;

        protected override void OnCreate()
        {
            // local
            inputPlayerGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedPlayerInput.Component>(),
                ComponentType.ReadWrite<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<CameraTransform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(inputPlayerGroup).ForEach(action);
        }

        private void Query(ref CameraTransform cameraTransform,
                                                     ref AdvancedPlayerInput.Component playerInput,
                                                     ref AdvancedUnitController.Component unitController,
                                                     ref SpatialEntityId entityId) 
        {
            var input = InputUtils.GetMove();
            var inputCam = InputUtils.GetCamera();
            var isShiftDown = Input.GetKey(KeyCode.LeftShift);
            var isJump = Input.GetKey(KeyCode.Space);
            var controller = playerInput.LocalController;
            if (CommonUpdate(input, inputCam, isShiftDown, isJump, entityId, ref controller) == false)
                return;

            playerInput.LocalController = controller;
            unitController.Controller = controller;
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class AdvancedUnmannedInputSync : AdvancedInputSync
    {
        private EntityQuery inputUnmannedGroup;
        private EntityQueryBuilder.F_DDD<BaseUnitStatus.Component, AdvancedUnmannedInput.Component, SpatialEntityId> action;

        protected override void OnCreate()
        {
            // unmanned
            inputUnmannedGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedUnmannedInput.Component>(),
                ComponentType.ReadOnly<AdvancedUnmannedInput.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(inputUnmannedGroup).ForEach(action);
        }

        private void Query(ref BaseUnitStatus.Component status,
                           ref AdvancedUnmannedInput.Component unMannedInput,
                           ref SpatialEntityId entityId)
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
            if (CommonUpdate(new Vector2(x, y), new Vector2(x, y), isShiftDown, isJump, entityId, ref controller))
                unMannedInput.LocalController = controller;
        }
    }

    internal abstract class AdvancedInputSync : SpatialComponentSystem
    {
        private const float MinInputChange = 0.01f;
        private const float MinInputCamera = 0.01f;

        protected bool CommonUpdate(in Vector2 inputPos, in Vector3 inputCam, bool isShiftDown, bool isJump, in SpatialEntityId entityId, ref ControllerInfo oldController)
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
                return true;
            }

            return false;
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
