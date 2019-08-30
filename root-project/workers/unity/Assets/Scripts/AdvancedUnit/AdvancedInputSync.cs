using System;
using Improbable.Gdk.Core;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class AdvancedInputSync : SpatialComponentSystem
    {
        private const float MinInputChange = 0.01f;

        private EntityQuery inputLocalGroup;
        private EntityQuery inputUnmannedGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            // local
            inputLocalGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedPlayerInput.Component>(),
                ComponentType.ReadWrite<CameraTransform>(),
                ComponentType.ReadOnly<AdvancedPlayerInput.ComponentAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            inputLocalGroup.SetFilter(AdvancedPlayerInput.ComponentAuthority.Authoritative);

            // unmanned
            inputUnmannedGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedUnmannedInput.Component>(),
                ComponentType.ReadOnly<AdvancedUnmannedInput.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            inputUnmannedGroup.SetFilter(AdvancedUnmannedInput.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleLocalInput();
            HandleUnmannedInput();
        }

        private void HandleLocalInput()
        {
            Entities.With(inputLocalGroup).ForEach((ref CameraTransform cameraTransform,
                                                    ref AdvancedPlayerInput.Component playerInput,
                                                    ref SpatialEntityId entityId) =>
            {
                var forward = cameraTransform.Rotation * Vector3.up;
                var right = cameraTransform.Rotation * Vector3.right;
                var input = InputUtils.GetMove(right, forward);
                var isShiftDown = Input.GetKey(KeyCode.LeftShift);
                CommonUpdate(input, isShiftDown, entityId, ref playerInput.LocalController);
            });
        }

        private void HandleUnmannedInput()
        {
            Entities.With(inputLocalGroup).ForEach((ref BaseUnitStatus.Component status,
                                                    ref AdvancedUnmannedInput.Component unamannedInput,
                                                    ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var time = Time.time;
                var inter = unmannedInput.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                var x = UnityEngine.Random.Range(-1.0f, 1.0f);
                var z = UnityEngine.Random.Range(-1.0f, 1.0f);
                var isShiftDown = false;//Input.GetKey(KeyCode.LeftShift);
                CommonUpdate(new Vector3(x,0,z), isShiftDown, entityId, ref unamannedInput.LocalController);
            });
        }

        private void CommonUpdate(in Vector3 input, bool isShiftDown, in SpatialEntityId entityId, ref ControllerInfo oldController)
        {
            if (Math.Abs(oldController.Horizontal - input.x) > MinInputChange
                || Math.Abs(oldController.Vertical - input.z) > MinInputChange
                || oldController.Running != isShiftDown)
            {
                var newContoroller = new ContorollerInfo
                {
                    Horizontal = input.x,
                    Vertical = input.z,
                    Running = isShiftDown
                };
                oldController = newController;
                UpdateSystem.SendEvent(new AdvancedUnitController.ControllerChanged.Event(newController), entityId.EntityId);
            }
        }
    }
}
