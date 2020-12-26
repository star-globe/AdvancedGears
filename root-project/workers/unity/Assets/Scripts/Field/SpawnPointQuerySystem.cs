using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class SpawnInfo
    {
        public EntityId EntityId;
        public UnitSide Side;
        public Coordinates Position;
        public SpawnType SpawnType;
    }

    public class SpawnPointQuerySystem : EntityQuerySystem
    {
        readonly Dictionary<UnitSide,Dictionary<EntityId,SpawnInfo>> spawnPointsDic = new Dictionary<UnitSide, Dictionary<EntityId, SpawnInfo>>();

        protected override bool IsCheckTime
        {
            get { return false; }
        }

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                var list = new IConstraint[]
                {
                    new ComponentConstraint(SpawnPoint.ComponentId),
                };

                return new ImprobableEntityQuery()
                {
                    Constraint = new AndConstraint(list),
                    ResultType = new SnapshotResultType()
                };
            }
        }

        protected override void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            if (shots.Count > 0)
            {
                SetSpawnPoints(shots);
            }
            else
            {
                SetSpawnPointsClear();
            }

            Debug.LogFormat("EntitySnapshotCount:{0}", shots.Count);
        }

        private void SetSpawnPoints(Dictionary<EntityId, List<EntitySnapshot>> snapShots)
        {
            foreach (var kvp in snapShots)
            {
                var snapList = kvp.Value;
                foreach (var snap in snapList)
                {
                    Position.Snapshot position;
                    if (snap.TryGetComponentSnapshot(out position) == false)
                        continue;

                    BaseUnitStatus.Snapshot status;
                    if (snap.TryGetComponentSnapshot(out status) == false)
                        continue;

                    if (status.State != UnitState.Alive)
                        continue;

                    SpawnPoint.Snapshot spawn;
                    if (snap.TryGetComponentSnapshot(out spawn) == false)
                        continue;

                    var side = status.Side;
                    Dictionary<EntityId, SpawnInfo> posDic;
                    if (spawnPointsDic.ContainsKey(side) == false)
                        posDic = new Dictionary<EntityId, SpawnInfo>();
                    else
                        posDic = spawnPointsDic[side];

                    if (posDic.ContainsKey(kvp.Key))
                        continue;

                    posDic.Add(kvp.Key, new SpawnInfo()
                    {
                        Side = status.Side,
                        EntityId = kvp.Key,
                        Position = position.Coords,
                        SpawnType = spawn.Type
                    });

                    spawnPointsDic[side] = posDic;
                }
            }
        }

        private void SetSpawnPointsClear()
        {
            spawnPointsDic.Clear();
        }

        public void RequestGetNearestSpawn(UnitSide side, SpawnType type, Coordinates coordinates, Action<Coordinates?> callBack)
        {
            if (spawnPointsDic.Count == 0)
            {
                OnQueriedEvent += () => GetNearestSpawn(side, type, coordinates, callBack);
                SendEntityQuery();
            }
            else
            {
                GetNearestSpawn(side, type, coordinates, callBack);
            }
        }

        private void GetNearestSpawn(UnitSide side, SpawnType type, Coordinates coordinates, Action<Coordinates?> callBack)
        {
            if (spawnPointsDic.TryGetValue(side, out var dic) == false)
            {
                callBack(null);
                return;
            }

            double length = double.MaxValue;
            Coordinates? target = null;
            foreach (var kvp in dic)
            {
                if (kvp.Value.Side != side ||
                    kvp.Value.SpawnType != type)
                    continue;

                var diff = coordinates - kvp.Value.Position;
                var mag = diff.SqrMagnitude();
                if (mag < length) {
                    target = kvp.Value.Position;
                    length = mag;
                }
            }

            callBack(target);
        }
    }
}
