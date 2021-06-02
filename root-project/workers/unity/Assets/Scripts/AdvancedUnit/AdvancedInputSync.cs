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
        private EntityQueryBuilder.F_DDDD<CameraTransform, AdvancedUnitController.Component, LocalController> action;

        protected override void OnCreate()
        {
            base.OnCreate();
            // local
            inputPlayerGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<AdvancedUnitController.HasAuthority>(),
                ComponentType.ReadOnly<CameraTransform>(),
                ComponentType.ReadOnly<LocalController>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            Entities.With(inputPlayerGroup).ForEach(action);
        }

        private void Query(ref CameraTransform cameraTransform,
                           ref AdvancedUnitController.Component unitController,
                           ref LocalController local) 
        {
            var input = InputUtils.GetMove();
            var inputCam = InputUtils.GetCamera();
            var isShiftDown = Input.GetKey(KeyCode.LeftShift);
            var isJump = Input.GetKey(KeyCode.Space);
            var action = local.Action;
            var stick = local.Stick;
            if (CommonUpdate(input, inputCam, isShiftDown, isJump, false, false, ref stick, ref action))
                unitController.Action = action;

            local.Stick = stick;
            local.Action = action;
        }

        protected override bool CheckOwner(Entity entity)
        {
            if (TryGetComponent<PlayerInfo.Component>(entity, out var comp) == false)
                return false;
            
            return string.Equals(comp.Value.ClientWorkerId, this.Worker.WorkerId);
        }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(SpatialOSUpdateGroup))]
    internal class AdvancedUnmannedInputSync : AdvancedInputSync
    {
        private EntityQuery inputUnmannedGroup;
        private EntityQueryBuilder.F_DDD<BaseUnitStatus.Component, AdvancedUnmannedInput.Component, AdvancedUnitController.Component, LocalController> action;

        protected override void OnCreate()
        {
            base.OnCreate();

            // unmanned
            inputUnmannedGroup = GetEntityQuery(
                ComponentType.ReadWrite<AdvancedUnmannedInput.Component>(),
                ComponentType.ReadOnly<AdvancedUnmannedInput.HasAuthority>(),
                ComponentType.ReadWrite<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<AdvancedUnitController.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<LocalController>()
            );

            action = Query;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Entities.With(inputUnmannedGroup).ForEach(action);
        }

        private void Query(ref BaseUnitStatus.Component status,
                           ref AdvancedUnmannedInput.Component unMannedInput,
                           ref AdvancedUnitController.Component controller,
                           ref LocalController local)
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
            var action = local.Action;
            var stick = local.Stick;
            if (CommonUpdate(new Vector2(x, y), new Vector2(x, y), isShiftDown, isJump, false, false, ref stick, ref action))
                controller.Action = action;
            
            local.Action = action;
            local.Stick = stick;
        }

        protected override bool CheckOwner(Entity entity)
        {
            return EntityManager.HasComponent<AdvancedUnmannedInput.Component>(entity);
        }
    }

    internal abstract class AdvancedInputSync : BaseEntitySearchSystem
    {
        public struct DummyController : IComponentData
        {
        }

        private const float MinInputChange = 0.01f;
        private const float MinInputCamera = 0.01f;

        private EntityQuery newControllerGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            newControllerGroup = GetEntityQuery(
                ComponentType.ReadOnly<AdvancedUnitController.Component>(),
                ComponentType.ReadOnly<AdvancedUnitController.HasAuthority>(),
                ComponentType.Exclude<LocalController>(),
                ComponentType.Exclude<DummyController>()
            );
        }

        protected override void OnUpdate()
        {
            HandleNews();
        }

        protected bool CommonUpdate(in Vector2 inputPos, in Vector3 inputCam, bool isShiftDown, bool isJump, bool isRightClick, bool isLeftClick,
                                    ref StickControllerInfo oldStick, ref ActionControllerInfo oldAction)
        {
            if (CheckChange(oldStick.Horizontal, inputPos.x) ||
                CheckChange(oldStick.Vertical, inputPos.y) ||
                CheckChange(oldStick.Yaw, inputCam.x) ||
                CheckChange(oldStick.Pitch, inputCam.y))
            {
                var newStick = new StickControllerInfo
                {
                    Horizontal = inputPos.x,
                    Vertical = inputPos.y,
                    Yaw = inputCam.x,
                    Pitch = inputCam.y
                };
                oldStick = newStick;
            }

            if (oldAction.Running != isShiftDown ||
                oldAction.Jump != isJump ||
                oldAction.LeftClick != isLeftClick ||
                oldAction.RightClick != isRightClick)
            {
                var newAction= new ActionControllerInfo
                {
                    Running = isShiftDown,
                    Jump = isJump,
                    LeftClick = isLeftClick,
                    RightClick = isRightClick, 
                };
                oldAction = newAction;
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

        private void HandleNews()
        {
            using (var newControllerEntities = this.newControllerGroup.ToEntityArray(Allocator.TempJob))
            {
                foreach (var entity in newControllerEntities)
                {
                    var owner = CheckOwner(entity);
                    if (owner) {
                        var controller = new LocalController
                        {
                            Stick = new StickControllerInfo(),
                            Action = new ActionControllerInfo(),
                        };
                        PostUpdateCommands.AddComponent(entity, controller);
                    }
                    else 
                        PostUpdateCommands.AddComponent(entity, new DummyController());
                }
            }
        }

        protected abstract bool CheckOwner(Entity entity);
    }

    public struct LocalController : IComponentData
    {
        public StickControllerInfo Stick;
        public ActionControllerInfo Action;
    }
}
