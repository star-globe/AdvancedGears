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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    class SymbolicObjectSystem : SpatialComponentSystem
    {
        IntervalChecker? interval = null;

        private GameObject rootObject;

        private EntityQuery groupObject;
        private EntityQueryBuilder.F_EC<SymbolicObject> objectAction;

        private EntityQuery groupPlayer;
        private EntityQueryBuilder.F_EDC<PlayerInfo.Component, Transform> playerAction;

        Vector3? playerPosition = null;
        float minLength = float.MaxValue;

        const float speed = 10.0f;
        const float minInter = 1/10;
        const float minLimitLength = 0.1f;

        protected override void OnCreate()
        {
            base.OnCreate();

            groupObject = GetEntityQuery(
                ComponentType.ReadOnly<SymbolicObject>(),
                ComponentType.ReadOnly<Position.Component>()
            );

            groupPlayer = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            objectAction = ObjectQuery;
            playerAction = PlayerQuery;
        }

        protected override void OnUpdate()
        {
            if (interval != null) {
                var inter = interval.Value;
                if (CheckTime(ref inter) == false)
                    return;
            }

            UpdatePlayerPosition();
            var min = UpdateObjects();

            if (min < float.MaxValue && min > 0)
                interval = IntervalCheckerInitializer.InitializedChecker(min);
        }

        private void UpdatePlayerPosition()
        {
            Entities.With(groupPlayer).ForEach(playerAction);
        }

        private void PlayerQuery(Unity.Entities.Entity entity,
                                    ref PlayerInfo.Component playerInfo,
                                    Transform transform)
        {
            if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                return;

            playerPosition = transform.position;
        }

        private float UpdateObjects()
        {
            if (playerPosition == null)
                return 0;

            minLength = float.MaxValue;

            Entities.With(groupObject).ForEach(objectAction);

            if (minLength == float.MaxValue)
                return float.MaxValue;

            return Mathf.Max(minLength / speed, minInter);
        }

        private void ObjectQuery(Entity entity,
                                SymbolicObject symbolic)
        {
            var trans = symbolic.transform;

            var diff = trans.position - playerPosition.Value;
            var length = Mathf.Max(minLimitLength, diff.magnitude);

            var rate = Mathf.Max(TowerDictionary.DispLength / length, TowerDictionary.ScaleRate);
            rate = Mathf.Min(rate, 1.0f);

            if (length < minLength)
                minLength = length;

            var scaledPos = rate * diff + playerPosition.Value;

            symbolic.SetRendererPosScale(scaledPos, Vector3.one * rate);
        }
    }
}
