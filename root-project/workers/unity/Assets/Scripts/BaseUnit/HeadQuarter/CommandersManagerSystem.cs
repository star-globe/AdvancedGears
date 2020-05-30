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
    class CommandersManagerSystem : BaseSearchSystem
    {
        class StrongInfo
        {
            public UnitSide side;
            public Coordinates coords;

            public StrongInfo(UnitSide s, Coordinates c)
            {
                side = s;
                coords = c;
            }
        }

        private EntityQuery commanderGroup;
        private EntityQuery strongholdGroup;

        private Dictionary<EntityId, StrongInfo> strongDic = null;
        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            commanderGroup = GetEntityQuery(
                ComponentType.ReadWrite<CommandersManager.Component>(),
                ComponentType.ReadOnly<CommandersManager.HasAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            strongholdGroup = GetEntityQuery(
                ComponentType.ReadOnly<StrongholdStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(10.0f);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter))
                UpdateStrongHolds();

            UpdateForCreateCommander();
        }

        void UpdateForCreateCommander()
        {
            Entities.With(commanderGroup).ForEach((Entity entity,
                                          ref CommandersManager.Component manager,
                                          ref BaseUnitStatus.Component status,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Type != UnitType.HeadQuarter)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                var inter = manager.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                manager.Interval = inter;

                List<EntityId> allies, enemies;
                GetStrongholdEntity(status.Side, position.Coords, out allies, out enemies);

                var pos = position.Coords.ToUnityVector() + this.Origin;

                int allyIndex = 0, enemyIndex = 0;
                uint rank = 0;

                var dic = manager.CommanderDatas.ToList();
                foreach (var kvp in dic)
                {
                    //if (kvp.Value.State != UnitState.Alive)
                    //    continue;

                    var r = kvp.Value.Rank;
                    if (r > rank)
                        rank = r;

                    var team = kvp.Value;
                    if (SelectTarget(ref enemyIndex, ref team.TargetEntityId, enemies))
                    {
                        manager.CommanderDatas[kvp.Key] = team;

                        var info = strongDic[team.TargetEntityId];

                        var request = new CommanderTeam.SetTargetStroghold.Request(kvp.Key, new TargetStrongholdInfo()
                        {
                            StrongholdId = team.TargetEntityId,
                            Position = info.coords,
                            Side = info.side,
                        });

                        this.CommandSystem.SendCommand(request);
                    }
                }

                if (manager.State == CommanderManagerState.CreateCommander)
                    return;

                if (rank < manager.MaxRank)
                {
                    var tgtId = manager.FactoryId;
                    if (SelectTarget(ref allyIndex, ref tgtId, allies))
                        manager.FactoryId = tgtId;

                    if (manager.FactoryId.IsValid())
                    {
                        var factoryId = manager.FactoryId;
                        var id = entityId.EntityId;
                        var request = new UnitFactory.AddSuperiorOrder.Request(factoryId, new SuperiorOrder()
                        {
                            Followers = new List<EntityId>(),
                            //HqEntityId = id,
                            Side = status.Side,
                            Rank = rank + 1
                        });
                        Entity factory;
                        if (TryGetEntity(factoryId, out factory))
                        {
                            this.CommandSystem.SendCommand(request, factory);
                            manager.State = CommanderManagerState.CreateCommander;
                        }
                    }
                }
            });
        }

        void UpdateStrongHolds()
        {
            strongDic = strongDic ?? new Dictionary<EntityId, StrongInfo>();

            Entities.With(strongholdGroup).ForEach((Entity entity,
                                  ref StrongholdStatus.Component stronghold,
                                  ref BaseUnitStatus.Component status,
                                  ref Position.Component position,
                                  ref SpatialEntityId spatialEntityId) =>
            {
                var entityId = spatialEntityId.EntityId;
                if (strongDic.ContainsKey(entityId))
                    strongDic[entityId].side = status.Side;
                else
                    strongDic.Add(entityId, new StrongInfo(status.Side, position.Coords));
            });
        }

        bool SelectTarget(ref int index, ref EntityId targetId, List<EntityId> list)
        {
            if (list.Count == 0 || targetId.IsValid())
                return false;

            if (index >= list.Count)
                index = 0;

            targetId = list[index];
            index++;

            return true;
        }

        readonly Dictionary<EntityId, double> allyDic = new Dictionary<EntityId, double>();
        readonly Dictionary<EntityId, double> enemyDic = new Dictionary<EntityId, double>();

        void GetStrongholdEntity(UnitSide side, Coordinates coords, out List<EntityId> allies, out List<EntityId> enemies)
        {
            if (strongDic == null)
                UpdateStrongHolds();

            allyDic.Clear();
            enemyDic.Clear();

            foreach (var kvp in strongDic)
            {
                var length = (kvp.Value.coords - coords).SqrMagnitude();

                if (kvp.Value.side == side)
                    allyDic.Add(kvp.Key, length);
                else
                    enemyDic.Add(kvp.Key, length);
            }

            allies = allyDic.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
            enemies = enemyDic.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
        }
    }
}
