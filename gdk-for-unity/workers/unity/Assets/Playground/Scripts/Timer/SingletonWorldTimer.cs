using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace Playground
{
    public class SingletonWorldTimer : MonoBehaviour
    {
        [Require] WorldTimerReader reader;
        [Require] World world;

        WorkerSystem worker;

        OnEnable()
        {
            reader.OnUpdatesEvent += TimerUpdated;
            worker = World.GetExistingSystem<WorkerSystem>();
            
            TimerUpdated(reader.Data.CurrentTime);
        }

        private void TimerUpdated(TimerInfo info)
        {
            if (worker == null)
                return;

            WorkerSingleton.Instance.SetTimer(worker.WorkerId, info);
        }
    }

    public class WorkerSingleton
    {
        private static readonly WorkerSingleton instance = new WorkerSingleton();

        public static WorkerSingleton Instance { get { return instance; } }

        readonly Dictionary<string,TimerInfo> timerDic = new Dictionary<string,TimerInfo>();

        public void SetTimer(string workerName, TimerInfo info)
        {
            if (timerDic.ContainsKey(workerName))
                timerDic[workerName] = info;
            else
                timerDic.Add(workerName, info);
        }

        public TimerInfo? GetTimer(string workerName)
        {
            if (timerDic.ContainsKey(workerName))
                return timerDic[workerName];

            return null;
        }
    }
}
