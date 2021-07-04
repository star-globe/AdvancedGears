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
        private EntityQueryBuilder.F_EDD<SymbolicTower.Component, Position.Component> towerAction;

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

            rootObject = new GameObject("TowerObjects");

            groupTower = GetEntityQuery(
                ComponentType.ReadOnly<SymbolicTower.Component>(),
                ComponentType.ReadOnly<Position.Component>()
            );

            groupPlayer = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Transform>()
            );

            towerAction = TowerQuery;
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
            var min = UpdateTowers();

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

        private float UpdateTowers()
        {
            if (playerPosition == null)
                return 0;

            minLength = float.MaxValue;

            Entities.With(groupTower).ForEach(towerAction);

            if (minLength == float.MaxValue)
                return float.MaxValue;

            return Mathf.Max(minLength / speed, minInter);
        }

        private void TowerQuery(Entity entity,
                                ref SymbolicTower.Component tower,
                                ref Position.Component position)
        {
            var settings = TowerDictionary.Get(tower.Side);
            if (settings == null)
            {
                Debug.LogErrorFormat("There is no Tower Settings. Side:{0}", tower.Side);
            }

            GameObject towerObj;
            if (towerObjDic.ContainsKey(tower.Side) == false)
            {
                towerObj = GameObject.Instantiate(settings.TowerObject);
                towerObj.transform.SetParent(rootObject.transform, false);
                towerObjDic[tower.Side] = towerObj;
            }
            else
            {
                towerObj = towerObjDic[tower.Side];
            }

            var t_pos = position.Coords.ToUnityVector() + this.Origin;
            var diff = t_pos - playerPosition.Value;
            var length = Mathf.Max(minLimitLength, diff.magnitude);

            if (minLength > length)
                minLength = length;

            var rate = Mathf.Min(1.0f, TowerDictionary.DispLength / length);

            var scaledPos = rate * diff + playerPosition.Value;

            towerObj.transform.localScale = Vector3.one * rate;
            towerObj.transform.position = scaledPos;
        }
    }
}
