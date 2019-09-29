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
                ComponentType.ReadOnly<AdvancedPlayerInput.ComponentAuthority>(),
                ComponentType.ReadOnly<SpatialEntityId>()

            );
            inputPlayerGroup.SetFilter(AdvancedPlayerInput.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(inputPlayerGroup).ForEach((ref CameraTransform cameraTransform,
                                                     ref AdvancedPlayerInput.Component playerInput,
                                                     ref SpatialEntityId entityId) =>
            {
                var forward = Vector3.forward;
                var right = Vector3.right;
                var input = InputUtils.GetMove(right, forward);
                var isShiftDown = Input.GetKey(KeyCode.LeftShift);
                var controller = playerInput.LocalController;
                CommonUpdate(input, isShiftDown, entityId, ref controller);
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
                ComponentType.ReadOnly<AdvancedUnmannedInput.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            inputUnmannedGroup.SetFilter(AdvancedUnmannedInput.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(inputUnmannedGroup).ForEach((ref BaseUnitStatus.Component status,
                                                       ref AdvancedUnmannedInput.Component unMannedInput,
                                                       ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var time = Time.time;
                var inter = unMannedInput.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                unMannedInput.Interval = inter;
                var x = UnityEngine.Random.Range(-1.0f, 1.0f);
                var z = UnityEngine.Random.Range(-1.0f, 1.0f);
                var isShiftDown = false;//Input.GetKey(KeyCode.LeftShift);
                var controller = unMannedInput.LocalController;
                CommonUpdate(new Vector3(x, 0, z), isShiftDown, entityId, ref controller);
                unMannedInput.LocalController = controller;
            });
        }
    }

    internal abstract class AdvancedInputSync : SpatialComponentSystem
    {
        private const float MinInputChange = 0.01f;

        protected void CommonUpdate(in Vector3 input, bool isShiftDown, in SpatialEntityId entityId, ref ControllerInfo oldController)
        {
            if (Math.Abs(oldController.Horizontal - input.x) > MinInputChange
                || Math.Abs(oldController.Vertical - input.z) > MinInputChange
                || oldController.Running != isShiftDown)
            {
                var newController = new ControllerInfo
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
