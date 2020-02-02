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
    public abstract class EntityQuerySystem : BaseEntitySearchSystem
    {
        IntervalChecker inter;
        IntervalChecker retryInter;
        private int queryRetries;
        private long? entityQueryId;

        public event Action OnQueriedEvent;

        protected virtual float IntervalTime { get { return 10.0f; } }
        protected virtual bool IsCheckTime { get { return true; } }
        protected virtual bool OtherCheck { get { return true; } }
        protected virtual int MaxQueryRetries { get { return 4; } }

        protected abstract ImprobableEntityQuery EntityQuery { get; }

        protected override void OnCreate()
        {
            base.OnCreate();

            inter = IntervalCheckerInitializer.InitializedChecker(IntervalTime, setChecked: true);
            retryInter = IntervalCheckerInitializer.InitializedChecker(1.0f, setChecked: true);
        }

        protected override void OnUpdate()
        {
            if (this.CommandSystem == null)
                return;

            if (entityQueryId != null)
            {
                if (retryInter.CheckTime() == false)
                    HandleEntityQueryResponses();
                else
                    SendEntityQuery();

                return;
            }

            if(IsCheckTime)
                return;

            if (inter.CheckTime() == false)
                return;

            // position check 
            if (OtherCheck == false)
                return;

            SendEntityQuery();
        }

        protected virtual void SendEntityQuery()
        {
            var entityQuery = this.EntityQuery;

            entityQueryId = this.CommandSystem.SendCommand(new WorldCommands.EntityQuery.Request
            {
                EntityQuery = entityQuery
            });

            DebugUtils.LogFormatColor(UnityEngine.Color.magenta, "Request.QueryID:{0}", entityQueryId);

            retryInter.UpdateLastChecked();
        }

        private void HandleEntityQueryResponses()
        {
            var entityQueryResponses = this.CommandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
            var name = this.GetType().ToString();
            var time = Time.time;

            //DebugUtils.LogFormatColor(UnityEngine.Color.magenta, "Name:{0} EntityQueryResponses.Count:{1} Time:{2}", name, entityQueryResponses.Count, time);
            for (var i = 0; i < entityQueryResponses.Count; i++)
            {
                ref readonly var response = ref entityQueryResponses[i];
                if (entityQueryId == null || response.RequestId != entityQueryId.Value)
                {
                    continue;
                }

                DebugUtils.LogFormatColor(UnityEngine.Color.magenta, "Name:{0} Response.RequestId:{1} Time:{2}", name, response.RequestId, time);

                entityQueryId = null;

                if (response.StatusCode == StatusCode.Success)
                {
                    var snapShots = new Dictionary<EntityId, List<EntitySnapshot>>();
                    foreach (var kvp in response.Result) {
                        List<EntitySnapshot> list;
                        if (snapShots.ContainsKey(kvp.Key))
                            list = snapShots[kvp.Key];
                        else
                            list = new List<EntitySnapshot>();

                        list.Add(kvp.Value);
                        snapShots[kvp.Key] = list;
                    }

                    Debug.LogFormat("Number of Snapshots. {0}:", snapShots.Count);

                    ReceiveSnapshots(snapShots);

                    OnQueriedEvent?.Invoke();
                    OnQueriedEvent = null;

                    queryRetries = 0;
                }
                else if (queryRetries < MaxQueryRetries)
                {
                    ++queryRetries;

                    this.LogDispatcher.HandleLog(LogType.Warning, new LogEvent(
                        string.Format("Retrying {0} query, attempt {1}.\n{2}", this.GetType().Name, queryRetries, response.Message)
                    ));

                    SendEntityQuery();
                }
                else
                {
                    this.LogDispatcher.HandleLog(LogType.Error, new LogEvent(
                        string.Format("Unable to get {0} query, after {1} attempts.", this.GetType().Name, queryRetries)
                    ));
                }
            }
        }

        protected virtual void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {

        }
    }
}


