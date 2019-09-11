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
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQueryServerSystem : FieldQueryBaseSystem
    {
        protected override Vector3 BasePosition { get { return this.WorkerSystem.Origin; } }
        protected override float SearchRadius { get { return 1000.0f; } }
        protected override bool CheckRegularly { get { return false; } }
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQueryClientSystem : FieldQueryBaseSystem
    {
        Vector3? playerPosition = null;
        protected override Vector3 BasePosition { get { return playerPosition != null ? playerPosition.Value: this.WorkerSystem.Origin; } }
        protected override float SearchRadius { get { return 500.0f; } }
        protected override bool CheckRegularly { get { return true; } }

        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(10.0f);
        private Unity.Entities.EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref PlayerInfo.Component playerInfo,
                                          ref Position.Component position) =>
            {
                var time = Time.time;
                if (inter.CheckTime(time) == false)
                    return;

                if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                    return;

                playerPosition = position.Coords.ToUnityVector() + this.Origin;
            });
        }
    }

    public abstract class FieldQueryBaseSystem : BaseEntitySearchSystem
    {
        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(10.0f);

        private int fieldQueryRetries;
        private long? fieldEntityQueryId;
        private readonly Dictionary<EntityId,List<EntitySnapshot>> fieldShanpShots = new Dictionary<EntityId,List<EntitySnapshot>>();

        const float fieldSize = 1000.0f;
        const float checkRange = 500.0f;
        Vector3? checkedPosition = null;

        private ImprobableEntityQuery fieldQuery;

        public FieldCreator FieldCreator { get; private set; }

        protected abstract Vector3 BasePosition { get; }
        protected abstract float SearchRadius { get; }
        protected abstract bool CheckRegularly { get; }
        protected override void OnCreate()
        {
            base.OnCreate();

            var go = new GameObject("FieldCreator");
            FieldCreator = go.AddComponent<FieldCreator>();
            FieldCreator.Setup(this.WorkerSystem.World, this.WorkerSystem.Origin);

            SendFieldEntityQuery();
        }

        protected override void OnUpdate()
        {
            if (fieldEntityQueryId != null)
            {
                HandleEntityQueryResponses();
                return;
            }

            if (fieldShanpShots.Count > 0)
            {
                foreach(var kvp in fieldShanpShots)
                    SetField(kvp.Key, kvp.Value);

                fieldShanpShots.Clear();
                return;
            }

            if (CheckRegularly == false)
                return;

            var time = Time.time;
            if (inter.CheckTime(time) == false)
                return;

            // position check 
            if (checkedPosition == null)
                return;

            var diff = checkedPosition.Value - BasePosition;
            if (diff.sqrMagnitude < checkRange * checkRange)
                return;

            SendFieldEntityQuery();
        }

        private void SendFieldEntityQuery()
        {
            checkedPosition = BasePosition;

            var list = new IConstraint[]
            {
                new ComponentConstraint(FieldComponent.ComponentId),
                new SphereConstraint(BasePosition.x, BasePosition.y, BasePosition.z, SearchRadius),
            };

            fieldQuery = new ImprobableEntityQuery()
            {
                Constraint = new AndConstraint(list),
                ResultType = new SnapshotResultType()
            };

            fieldEntityQueryId = this.CommandSystem.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = fieldQuery
            });
        }

        private void HandleEntityQueryResponses()
        {
            var entityQueryResponses = this.CommandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
            for (var i = 0; i < entityQueryResponses.Count; i++)
            {
                ref readonly var response = ref entityQueryResponses[i];
                if (response.RequestId != fieldEntityQueryId)
                {
                    continue;
                }

                fieldEntityQueryId = null;

                if (response.StatusCode == StatusCode.Success)
                {
                    foreach (var kvp in response.Result) {
                        List<EntitySnapshot> list;
                        if (fieldShanpShots.ContainsKey(kvp.Key))
                            list = fieldShanpShots[kvp.Key];
                        else
                            list = new List<EntitySnapshot>();

                        list.Add(kvp.Value);
                        fieldShanpShots[kvp.Key] = list;
                    }
                }
                else if (fieldQueryRetries < PlayerLifecycleConfig.MaxPlayerCreatorQueryRetries)
                {
                    ++fieldQueryRetries;

                    this.LogDispatcher.HandleLog(LogType.Warning, new LogEvent(
                        $"Retrying field query, attempt {fieldQueryRetries}.\n{response.Message}"
                    ));

                    SendFieldEntityQuery();
                }
                else
                {
                    var retryText = fieldQueryRetries == 0
                        ? "1 attempt"
                        : $"{fieldQueryRetries + 1} attempts";

                    this.LogDispatcher.HandleLog(LogType.Error, new LogEvent(
                        $"Unable to find player creator after {retryText}."
                    ));
                }

                break;
            }
        }

        private void SetField(EntityId entityId, List<EntitySnapshot> snapShots)
        {
            FieldCreator.Reset();

            foreach (var snap in snapShots)
            {
                Position.Snapshot position;
                if (snap.TryGetComponentSnapshot(out position) == false)
                    return;

                FieldComponent.Snapshot field;
                if (snap.TryGetComponentSnapshot(out field) == false)
                    return;

                FieldCreator.RealizeField(field.TerrainPoints, position.Coords, this.BasePosition);
            }

            FieldCreator.RemoveFields();
        }
    }
}


