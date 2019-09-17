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
        protected override Vector3? BasePosition => this.WorkerSystem.Origin;
        protected override float SearchRadius => 1000.0f;
        protected override bool CheckRegularly => false;
        protected override FieldWorkerType FieldWorkerType => FieldWorkerType.GameLogic;
    }

    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQueryClientSystem : FieldQueryBaseSystem
    {
        Vector3? playerPosition = null;
        protected override Vector3? BasePosition => playerPosition;
        protected override float SearchRadius => 500.0f;
        protected override bool CheckRegularly => true;
        protected override FieldWorkerType FieldWorkerType => FieldWorkerType.Client; 

        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(10.0f, setChecked:true);
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

            base.OnUpdate();
        }
    }

    public abstract class FieldQueryBaseSystem : BaseEntitySearchSystem
    {
        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(10.0f, setChecked: true);

        private int fieldQueryRetries;
        private long? fieldEntityQueryId;
        private readonly Dictionary<EntityId, List<EntitySnapshot>> fieldShanpShots = new Dictionary<EntityId, List<EntitySnapshot>>();

        float checkRange;
        Vector3? checkedPosition = null;

        private ImprobableEntityQuery fieldQuery;

        public FieldCreator FieldCreator { get; private set; }

        protected abstract Vector3? BasePosition { get; }
        protected abstract float SearchRadius { get; }
        protected abstract bool CheckRegularly { get; }
        protected abstract FieldWorkerType FieldWorkerType { get;}

        protected override void OnCreate()
        {
            base.OnCreate();

            var go = new GameObject("FieldCreator");
            FieldCreator = go.AddComponent<FieldCreator>();
            FieldCreator.Setup(this.WorkerSystem.World, this.WorkerSystem.Origin, this.WorkerSystem.WorkerId, FieldWorkerType);

            var settings = FieldCreator.Settings;
            checkRange = settings != null ? settings.FieldSize / 2 : 0;
            checkRange *= 0.8f;
        }

        protected override void OnUpdate()
        {
            if (this.BasePosition == null)
                return;

            if (fieldEntityQueryId != null)
            {
                HandleEntityQueryResponses();
                return;
            }

            if (CheckRegularly == false && FieldCreator.IsSetDatas)
                return;

            var time = Time.time;
            if (inter.CheckTime(time) == false)
                return;

            // position check 
            if (checkedPosition != null)
            {
                var diff = checkedPosition.Value - BasePosition.Value;
                if (diff.sqrMagnitude < checkRange * checkRange)
                    return;
            }

            SendFieldEntityQuery();
        }

        private void SendFieldEntityQuery()
        {
            checkedPosition = BasePosition;
            fieldShanpShots.Clear();

            var list = new IConstraint[]
            {
                new ComponentConstraint(FieldComponent.ComponentId),
                new SphereConstraint(BasePosition.Value.x, BasePosition.Value.y, BasePosition.Value.z, SearchRadius),
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

            Debug.LogFormat("SendFieldQuery. WorkerId:{0}", this.WorkerSystem.WorkerId);
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

                    if (fieldShanpShots.Count > 0) {
                        foreach(var kvp in fieldShanpShots)
                            SetField(kvp.Key, kvp.Value);
                    }
                    else {
                        SetFieldClear();
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

        private void SetFieldClear()
        {
            FieldCreator.Reset();
            FieldCreator.RealizeEmptyField(this.BasePosition);
            FieldCreator.RemoveFields();
        }
    }
}


