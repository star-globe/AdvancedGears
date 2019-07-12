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
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalTimerUpdateSystem : ComponentSystem
    {
        EntityQuery group;

        private TimerInfo? timer;
        public TimerInfo? Timer { get { return timer;} }

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
        }

        protected override void OnUpdate()
        {
            // TODO Events
        }

        public void SetTimer(TimerInfo info)
        {
            timer = info;
        }
    }
}
