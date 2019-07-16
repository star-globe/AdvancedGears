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

        LocalTimerUpdateSystem system;

        void OnEnable()
        {
            reader.OnUpdatesEvent += TimerUpdated;
            system = world.GetExistingSystem<LocalTimerUpdateSystem>();

            TimerUpdated(reader.Data.CurrentTime);
        }

        private void TimerUpdated(TimerInfo info)
        {
            if (system != null)
                system.SetTimer(info);
        }
    }
}
