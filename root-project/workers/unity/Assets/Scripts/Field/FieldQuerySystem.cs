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
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class FieldQuerySystem : SpatialComponentSystem
    {
        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(10.0f);

        private int fieldQueryRetries;
        private long? fieldEntityQueryId;
        private readonly List<EntitySnapshot> fieldShanpShots = new List<EntitySnapshot>();


        private readonly EntityQuery fieldQuery = new EntityQuery
        {
            Constraint = new ComponentConstraint(FieldComponent.ComponentId),
            ResultType = new SnapshotResultType()
        };

        private WorkerSystem worker;

        public FieldCreator FieldCreator { get; private set; }

        protected override void OnCreate()
        {
            base.OnCreate();

            worker = World.GetExistingSystem<WorkerSystem>();

            var go = new GameObject("FieldCreator");
            FieldCreator = go.AddComponent<FieldCreator>();
            FieldCreator.Setup(worker.World, worker.Origin);

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
                
                return;
            }

            var time = Time.time;
            if (inter.CheckTime(time) == false)
                return;

            fieldShanpShots.Clear();

            SendFieldEntityQuery();
        }

        private void SendFieldEntityQuery()
        {
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
                    fieldShanpShots.AddRange(response.Result.Values);
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

        private void SetField()
        {
            var snap = fieldShanpShots[UnityEngine.Random.Range(0, fieldShanpShots.Count)];

            Position.Snapshot position;
            if (snap.TryGetComponentSnapshot(out position) == false)
                return;

            FieldComponent.Snapshot field;
            if (snap.TryGetComponentSnapshot(out field) == false)
                return;

            FieldCreator.RealizeField(field.TerrainPoints, position.Coords);
        }
    }
}


