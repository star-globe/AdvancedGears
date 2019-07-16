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

        CommandSystem commandSystem;

        private TimerInfo? timer;
        public TimerInfo? Timer { get { return timer;} }

        bool connected = false;
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            commandSystem = World.GetExistingSystem<CommandSystem>();

            group = GetEntityQuery(
                ComponentType.ReadOnly<OnConnected>(),
                ComponentType.ReadOnly<WorkerEntityTag>()
            );
        }

        protected override void OnUpdate()
        {
            if (conntected)
                return;

            // TODO Events
            Entities.With(group).ForEach(entity =>
            {
                connected = true;
                //commandSystem.SendCommand();
            });
        }

        public void SetTimer(TimerInfo info)
        {
            timer = info;
        }
    }
}
