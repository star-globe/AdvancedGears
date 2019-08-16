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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalTimerUpdateSystem : BaseEntitySearchSystem
    {
        private ILogDispatcher logDispatcher;

        private TimerInfo? timerInfo;
        public TimerInfo? Timer { get { return timerInfo;} }

        private long? timerEntityQueryId;
        private readonly List<EntityId> timerEntityIds = new List<EntityId>();
        private readonly Dictionary<EntityId,EntitySnapshot> timerEntityDic = new Dictionary<EntityId, EntitySnapshot>();
        //private readonly InterestQuery timerQuery;
        private EntityQuery timerQuery;
        //InterestTemplate template;

        void Initialize()
        {
            Vector3? pos = Vector3.zero;
            double radius = 300;

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
            timerEntityQueryId = this.CommandSystem.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = timerQuery
            });
        }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            logDispatcher = this.LogDispatcher;

            Initialize();

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
            var entityQueryResponses = this.CommandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
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
