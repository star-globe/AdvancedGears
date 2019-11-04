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
using UnityEngine.Experimental.PlayerLoop;

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

        private Dictionary<SpatialEntityId, StrongInfo> strongDic = null;
        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            commanderGroup = GetEntityQuery(
                ComponentType.ReadWrite<CommandersManager.Component>(),
                ComponentType.ReadOnly<CommandersManager.ComponentAuthority>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            commanderGroup.SetFilter(CommandersManager.ComponentAuthority.Authoritative);

            strongholdGroup = GetEntityQuery(
                ComponentType.ReadOnly<StrongholdUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(10.0f);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime())
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

                if (manager.State == CommanderManagerState.CreateCommander)
                    return;

                var inter = manager.Interval;
                if (inter.CheckTime() == false)
                    return;

                manager.Interval = inter;

                List<SpatialEntityId> allies, enemies;
                GetStrongholdEntity(status.Side, position.Coords, out allies, out enemies);

                var pos = position.Coords.ToUnityVector() + this.Origin;

                int allyIndex = 0, enemyIndex = 0;
                uint rank = 0;

                var dic = manager.CommanderDatas.ToList();
                foreach (var kvp in dic)
                {
                    if (kvp.Value.State != UnitState.Alive)
                        continue;

                    var r = kvp.Value.Rank;
                    if (r > rank)
                        rank = r;

                    var team = kvp.Value;
                    if (SelectTarget(ref allyIndex, ref team.TargetStronghold, allies))
                    {
                        manager.CommanderDatas[kvp.Key] = team;

                        //var request = new 
                        //
                        //this.CommandSystem.SendCommand();
                    }
                }

                if (rank < manager.MaxRank)
                {
                    var tgtId = manager.FactoryId;
                    if (SelectTarget(ref enemyIndex, ref tgtId, enemies))
                        manager.FactoryId = tgtId;

                    if (manager.FactoryId.IsValid())
                    {
                        var factoryId = manager.FactoryId;
                        var id = entityId.EntityId;
                        var request = new UnitFactory.AddSuperiorOrder.Request(factoryId, new SuperiorOrder()
                        {
                            Followers = new List<EntityId>(),
                            HqEntityId = id,
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
            strongDic = strongDic ?? new Dictionary<SpatialEntityId, StrongInfo>();

            Entities.With(strongholdGroup).ForEach((Entity entity,
                                  ref StrongholdUnitStatus.Component stronghold,
                                  ref BaseUnitStatus.Component status,
                                  ref Position.Component position,
                                  ref SpatialEntityId entityId) =>
            {
                if (strongDic.ContainsKey(entityId))
                    strongDic[entityId].side = status.Side;
                else
                    strongDic.Add(entityId, new StrongInfo(status.Side, position.Coords));
            });
        }

        bool SelectTarget(ref int index, ref EntityId targetId, List<SpatialEntityId> list)
        {
            if (list.Count == 0 || targetId.IsValid())
                return false;

            if (index >= list.Count)
                index = 0;

            targetId = list[index].EntityId;
            index++;

            return true;
        }

        readonly Dictionary<SpatialEntityId, double> allyDic = new Dictionary<SpatialEntityId, double>();
        readonly Dictionary<SpatialEntityId, double> enemyDic = new Dictionary<SpatialEntityId, double>();

        void GetStrongholdEntity(UnitSide side, Coordinates coords, out List<SpatialEntityId> allies, out List<SpatialEntityId> enemies)
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
