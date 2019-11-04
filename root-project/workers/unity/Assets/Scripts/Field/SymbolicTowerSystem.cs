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
    class SymbolicTowerSystem : SpatialComponentSystem
    {
        readonly Dictionary<UnitSide, GameObject> towerObjDic = new Dictionary<UnitSide, GameObject>();
        IntervalChecker? interval = null;

        private GameObject rootObject;

        private EntityQuery groupTower;
        private EntityQuery groupPlayer;

        Vector3? playerPosition = null;

        protected override void OnCreate()
        {
            base.OnCreate();

            rootObject = new GameObject("TowerObjects");

            groupTower = GetEntityQuery(
                ComponentType.ReadOnly<SymbolicTower.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            groupPlayer = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            if (interval != null)
            {
                var inter = interval.Value;
                if (inter.CheckTime() == false)
                    return;
            }

            UpdatePlayerPosition();
            var min = UpdateTowers(interval.Value.Interval);

            if (min < float.MaxValue)
                interval = IntervalCheckerInitializer.InitializedChecker(min);
        }

        private void UpdatePlayerPosition()
        {
            Entities.With(groupPlayer).ForEach((Unity.Entities.Entity entity,
                                  ref PlayerInfo.Component playerInfo,
                                  ref Position.Component position) =>
            {
                if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                    return;

                playerPosition = position.Coords.ToUnityVector() + this.Origin;
            });
        }

        private float UpdateTowers(float inter)
        {
            if (playerPosition == null)
                return inter;

            float minLength = float.MaxValue;

            Entities.With(groupTower).ForEach((Entity entity,
                                          ref SymbolicTower.Component tower,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
            {
                var settings = TowerDictionary.Get(tower.Side);

                GameObject towerObj;
                if (towerObjDic.ContainsKey(tower.Side) == false)
                {
                    towerObj = GameObject.Instantiate(settings.TowerObject);
                    towerObj.transform.SetParent(rootObject.transform, false);
                }
                else
                {
                    towerObj = towerObjDic[tower.Side];
                }

                var t_pos = position.Coords.ToUnityVector() + this.Origin;
                var diff = t_pos - playerPosition.Value;
                var length = diff.magnitude;

                if (minLength > length)
                    minLength = length;

                var rate = length / TowerDictionary.DispLength;

                var scaledPos = rate * diff + playerPosition.Value;

                towerObj.transform.localScale = Vector3.one * rate;
                towerObj.transform.position = scaledPos;
            });

            return minLength;
        }
    }
}
