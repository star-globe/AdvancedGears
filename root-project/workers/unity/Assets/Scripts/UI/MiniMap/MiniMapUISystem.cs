using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using AdvancedGears;

namespace AdvancedGears.UI
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class MiniMapUISystem : BaseUISystem<MiniMapUIObject>
    {
        private EntityQuery playerGroup;
        private EntityQuery unitGroup;

        MiniMapUIDisplay MiniMapUIDisplay => MiniMapUIDisplay.Instance;

        protected override UIType uiType => UIType.MiniMapObject;
        protected override Transform parent
        {
            get { return this.MiniMapUIDisplay?.MiniMapParent; }
        }

        private Camera camera = null;
        private Camera Camera
        {
            get
            {
                camera = camera ?? Camera.main;
                return camera;
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            playerGroup = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            unitGroup = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        const float size = 1.1f;
        const float depth = 1000.0f;
        Bounds viewBounds = new Bounds(new Vector3(0.5f,0.5f, depth/2), new Vector3(size,size, depth));

        Vector3? playerPosition = null;

        private void SetUIObject(in BaseUnitStatus.Component status, in EntityId entityId, in Position.Component position)
        {
            if (playerPosition == null)
                return;

            var uiObject = this.GetOrCreateUI(entityId);
            if (uiObject == null)
                return;

            if (this.Camera == null)
                return;

            var diff = position.Coords.ToUnityVector() - playerPosition.Value;
            var inversed = this.Camera.transform.InverseTransformVector(diff);
            var vec2 = this.MiniMapUIDisplay.GetMiniMapPos(new Vector2(inversed.x, inversed.z));

            uiObject.SetInfo(vec2, status.Side, status.Type);
            uiObject.SetName(string.Empty);
        }

        void UpdatePlayerPositions()
        {
            Entities.With(playerGroup).ForEach((Entity entity,
                                          ref PlayerInfo.Component player,
                                          ref BaseUnitStatus.Component status,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
            {
                var pos = position.Coords.ToUnityVector();

                if (player.ClientWorkerId.Equals(this.WorkerSystem.WorkerId))
                {
                    playerPosition = position.Coords.ToUnityVector();
                }

                var minimapObject = this.GetOrCreateUI(entityId.EntityId);
                if (minimapObject != null)
                    minimapObject.SetName(player.Name);
            });
        }

        protected override void UpdateAction()
        {
            if (this.MiniMapUIDisplay == null)
                return;

            UpdatePlayerPositions();

            Entities.With(unitGroup).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
            {
                SetUIObject(status, entityId.EntityId, position);
            });
        }
    }
}
