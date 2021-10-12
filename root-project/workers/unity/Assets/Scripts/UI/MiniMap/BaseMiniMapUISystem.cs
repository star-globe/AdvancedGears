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
    abstract class BaseMiniMapUISystem<T> : BaseUISystem<T> where T : Component,IUIObject
    {
        MiniMapUIDisplay MiniMapUIDisplay => MiniMapUIDisplay.Instance;

        private EntityQuery playerGroup;
        private EntityQueryBuilder.F_ECDD<Transform, PlayerInfo.Component, SpatialEntityId> playerAction;

        protected override Transform parent
        {
            get { return this.MiniMapUIDisplay?.MiniMapParent; }
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            playerGroup = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            playerAction = PlayerQuery;
        }

        Vector3? playerPosition = null;

        void PlayerQuery(Entity entity,
                          Transform transform,
                          ref PlayerInfo.Component player,
                          ref SpatialEntityId entityId)
        {
            if (player.ClientWorkerId.Equals(this.WorkerSystem.WorkerId))
            {
                playerPosition = transform.position - this.Origin;
            }
        }

        protected T GetUIObject(in EntityId entityId, in Coordinates coords, out Vector2 vec2)
        {
            return GetUIObject(entityId, coords, out vec2, out var rot);
        }

        protected T GetUIObject(in EntityId entityId, in Coordinates coords, out Vector2 vec2, out float rot)
        {
            vec2 = Vector2.zero;
            rot = 0;
            if (playerPosition == null)
                return null;

            var uiObject = this.GetOrCreateUI(entityId);
            if (uiObject == null)
                return null;

            if (this.Camera == null)
                return null;

            var diff = coords.ToUnityVector() - playerPosition.Value;
            var inversed = this.Camera.transform.InverseTransformVector(diff);
            vec2 = this.MiniMapUIDisplay.GetMiniMapPos(new Vector2(inversed.x, inversed.z));
            var forward = this.Camera.transform.InverseTransformDirection(Vector3.forward);
            rot = Mathf.Atan2(forward.z, forward.x);
            return uiObject;
        }

        protected override void UpdateAction()
        {
            if (this.MiniMapUIDisplay == null)
                return;

            UpdatePlayerPositions();

            UpdateUIObject();
        }

        void UpdatePlayerPositions()
        {
            Entities.With(playerGroup).ForEach(playerAction);
        }

        protected abstract void UpdateUIObject();
    }
}
