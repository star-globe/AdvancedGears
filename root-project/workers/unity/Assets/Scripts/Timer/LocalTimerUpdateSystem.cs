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
    public class LocalTimerUpdateSystem : SpatialComponentBaseSystem
    {
        double updateTime;
        double baseTime;
        double buffer;
        
        public double CurrentTime
        {
            get { return buffer + updateTime; }
        }

        protected override void OnCreate()
        {
            UpdateCurrent(TimerUtils.CurrentTime);
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
            if (updatesEvents.Count > 0) {
                var i = UnityEngine.Random.Range(0,updatesEvents.Count);
                var timerEvent = updatesEvents[i];
                UpdateCurrent(timerEvent.Event.Payload.CurrentSeconds);
            }
        }

        private void UpdateCurrent(double seconds)
        {
            updateTime = seconds;
            baseTime = this.Time.ElapsedTime;
            buffer = 0;
        }
    }
}
