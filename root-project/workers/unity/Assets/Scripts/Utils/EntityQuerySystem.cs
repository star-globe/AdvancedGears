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
        private int queryRetries;
        private long? entityQueryId;
        private readonly Dictionary<EntityId, List<EntitySnapshot>> snapShots = new Dictionary<EntityId, List<EntitySnapshot>>();

        public event Action OnQueriedEvent;

        protected virtual float IntervalTime { get { return 10.0f; } }
        protected virtual bool IsCheckTime { get { return true; } }
        protected virtual bool OtherCheck { get { return true; } }
        protected virtual int MaxQueryRetries { get { return 4; } }

        protected abstract ImprobableEntityQuery EntityQuery { get; }

        protected override void OnCreate()
        {
            base.OnCreate();

            inter =  IntervalCheckerInitializer.InitializedChecker(IntervalTime, setChecked: true);
        }

        protected override void OnUpdate()
        {
            if (entityQueryId != null)
            {
                HandleEntityQueryResponses();
                return;
            }

            if(IsCheckTime == false)
                return;

            var time = Time.time;
            if (inter.CheckTime(time) == false)
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
        }

        private void HandleEntityQueryResponses()
        {
            var entityQueryResponses = this.CommandSystem.GetResponses<WorldCommands.EntityQuery.ReceivedResponse>();
            for (var i = 0; i < entityQueryResponses.Count; i++)
            {
                ref readonly var response = ref entityQueryResponses[i];
                if (response.RequestId != entityQueryId)
                {
                    continue;
                }

                entityQueryId = null;

                if (response.StatusCode == StatusCode.Success)
                {
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

                break;
            }
        }

        protected virtual void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {

        }
    }
}


