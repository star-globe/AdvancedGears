using System;
using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;

using Improbable.Gdk.Core.Commands;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using Improbable.Gdk.QueryBasedInterest;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using EntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalTimerUpdateSystem : BaseEntitySearchSystem
    {
        private ILogDispatcher logDispatcher;

        private TimerInfo? timerInfo;
        public TimerInfo? Timer { get { return timerInfo;} }

        private long? timerEntityQueryId;
        private readonly List<EntityId> timerEntityIds = new List<EntityId>();
        private readonly Dictionary<EntityId,EntityQuerySnapshot> timerEntityDic = new Dictionary<EntityId, EntityQuerySnapshot>();
        //private readonly InterestQuery timerQuery;
        private readonly EntityQuery timerQuery;
        //InterestTemplate template;

        public LocalTimerUpdateSystem(double radius = 300, Vector3? pos = null)
        {
            pos = pos ?? Vector3.zero;

            var list = new IConstraint[]
            {
                new ComponentConstraint(WorldTimer.ComponentId),
                new SphereConstraint(pos.Value.x, pos.Value.y, pos.Value.z, radius),
            };

            timerQuery = new EntityQuery()
            {
                Constraint = new AndConstraint(list),
                ResultType = new SnapshotResultType()
            };
        }


        private void SendTimerEntityQuery()
        {
            timerEntityQueryId = Command.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = timerQuery
            });
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            logDispatcher = base.Worker.LogDispatcher;

            SendTimerEntityQuery();
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
            var id = timerEntityIds[UnityEngine.Random.Range(0, timerEntityIds.Count)];
            WorldTimer.Component? timer = null;
            if (TryGetComponent(id, out timer) == false)
                return;

            SetTimer(timer.Value.CurrentTime);
        }

        public void SetTimer(TimerInfo info)
        {
            this.timerInfo = info;
        }

        int retries = 0;
        void HandleEntityQueryResponses()
        {
            var entityQueryResponses = Command.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
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
