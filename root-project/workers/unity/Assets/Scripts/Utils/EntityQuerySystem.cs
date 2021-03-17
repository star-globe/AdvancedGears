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
        private CommandRequestId? entityQueryId;

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
                if (CheckTime(ref retryInter) == false)
                    HandleEntityQueryResponses();
                else
                    SendEntityQuery();

                return;
            }

            if(IsCheckTime)
                return;

            if (CheckTime(ref inter) == false)
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

            UpdateLastChecked(ref retryInter);
        }

        readonly Dictionary<EntityId, List<EntitySnapshot>> resultSnapShots = new Dictionary<EntityId, List<EntitySnapshot>>();
        readonly Queue<List<EntitySnapshot>> listQueue = new Queue<List<EntitySnapshot>>();

        private void HandleEntityQueryResponses()
        {
            var entityQueryResponses = this.CommandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
            var name = this.GetType().ToString();
            var time = Time.ElapsedTime;

            for (var i = 0; i < entityQueryResponses.Count; i++)
            {
                ref readonly var response = ref entityQueryResponses[i];
                if (entityQueryId == null || response.RequestId != entityQueryId.Value)
                {
                    continue;
                }

                entityQueryId = null;

                if (response.StatusCode == StatusCode.Success)
                {
                    ClearSnapshots();

                    foreach (var kvp in response.Result) {
                        List<EntitySnapshot> list;
                        if (resultSnapShots.ContainsKey(kvp.Key))
                            list = resultSnapShots[kvp.Key];
                        else
                            list = listQueue.Count > 0 ? listQueue.Dequeue(): new List<EntitySnapshot>();

                        list.Add(kvp.Value);
                        resultSnapShots[kvp.Key] = list;
                    }

                    //Debug.LogFormat("Number of Snapshots. {0}:", snapShots.Count);

                    ReceiveSnapshots(resultSnapShots);

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

        private void ClearSnapshots()
        {
            foreach (var kvp in resultSnapShots) {
                listQueue.Enqueue(kvp.Value);
            }

            resultSnapShots.Clear();
        }
    }
}


