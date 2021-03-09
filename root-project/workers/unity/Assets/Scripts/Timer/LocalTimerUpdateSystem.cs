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

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalTimerUpdateSystem : SpatialComponentBaseSystem
    {
        double updateTime;
        double baseTime;
        double buffer;
        
        public double CurrentTime
        {
            get { return buffer + updatetime; }
        }

        protected override void OnCreate()
        {
            UpdateCurrent(DateTime.UtcNow.Ticks);
        }

        protected override void OnUpdate()
        {
            HandleEvents();
        }

        private void UpdateBuffer()
        {
            buffer = this.Time.ElapsedTime - baseTime;
        }

        private void HandleEvents()
        {
            var updatesEvents = UpdateSystem.GetEventsReceived<WorldTimer.Updates.Event>();
            for (var i = 0; i < updatesEvents.Count; i++)
            {
                var timerEvent = updatesEvents[i];
                UpdateCurrent(timerEvent.Event.Payload.CurrentTicks);
                break;
            }
        }

        private void UpdateCurrent(long ticks)
        {
            updateTime = (ticks / TimeSpan.TicksPerMillisecond) / 1000.0;
            baseTime = this.Time.ElapsedTime;
            buffer = 0;
        }
    }
}
