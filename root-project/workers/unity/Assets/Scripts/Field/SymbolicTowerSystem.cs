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
                var time = Time.time;
                var inter = interval.Value;
                if (inter.CheckTime(time) == false)
                    return;
            }

            UpdatePlayerPosition();
            UpdateTowers();
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

        private void UpdateTowers()
        {
            if (playerPosition == null)
                return;

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

                // position check
            });
        }
    }
}
