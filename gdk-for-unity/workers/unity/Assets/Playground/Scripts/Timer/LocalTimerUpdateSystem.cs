using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;

using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalTimerUpdateSystem : ComponentSystem
    {
        WorkerSystem workerSystem;
        CommandSystem commandSystem;
        private ILogDispatcher logDispatcher;

        private TimerInfo? timer;
        public TimerInfo? Timer { get { return timer;} }

        private long? timerEntityQueryId;
        private readonly List<EntityId> timerEntityIds = new List<EntityId>();
        private readonly EntityQuery timerQuery = new EntityQuery
        {
            Constraint = new ComponentConstraint(WorldTimer.ComponentId),
            ResultType = new SnapshotResultType()
        };

        private void SendTimerEntityQuery()
        {
            timerEntityQueryId = commandSystem.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = timerQuery
            });
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            workerSystem = World.GetExistingSystem<WorkerSystem>();
            commandSystem = World.GetExistingSystem<CommandSystem>();
            logDispatcher = workerSystem.LogDispatcher;

            SendTimerEntityQuery();
        }

        public void SetTimer(TimerInfo info)
        {
            timer = info;
        }

        protected override void OnUpdate()
        {
            if (timerEntityIds.Count > 0)
            {
                HandleSetTimer();
            }
            else
            {
                HandleEntityQueryResponses();
            }
        }

        void HandleSetTimer()
        {

        }

        int retries = 0;
        void HandleEntityQueryResponses()
        {
            var entityQueryResponses = commandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
            for (var i = 0; i < entityQueryResponses.Count; i++)
            {
                ref readonly var response = ref entityQueryResponses[i];
                if (response.RequestId != timerEntityQueryId)
                {
                    continue;
                }

                timerEntityQueryId = null;

                if (response.StatusCode == StatusCode.Success)
                {
                    timerEntityIds.AddRange(response.Result.Keys);
                }
                else if (retries < 3)
                {
                    ++retries;

                    logDispatcher.HandleLog(LogType.Warning, new LogEvent(
                        $"Retrying timer query, attempt {retries}.\n{response.Message}"
                    ));

                    SendTimerEntityQuery();
                }
                else
                {
                    var retryText = retries == 0
                        ? "1 attempt"
                        : $"{retries + 1} attempts";

                    logDispatcher.HandleLog(LogType.Error, new LogEvent(
                        $"Unable to find timer after {retryText}."
                    ));
                }

                break;
            }
        }
    }
}
