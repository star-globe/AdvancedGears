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
    internal class TimerSynchronizeSystem : ComponentSystem
    {
        EntityQuery group;

        private ComponentUpdateSystem updateSystem;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<WorldTimer.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        const int upInter = 300;

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref WorldTimer.Component timer,
                                          ref SpatialEntityId entityId) =>
            {
                if (UnityEngine.Random.Range(0,upInter) != 0)
                    return;

                var now = DateTime.UtcNow;
                var span = now - DateTime.MinValue;
                var start = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

                var sec = (now - start).TotalSeconds;
                var d = (int)span.TotalDays;

                timer.UnitTime = span.Ticks;
                var info = new TimerInfo
                {
                    Second = (float)sec,
                    Day = d,
                };

                timer.CurrentTime = info;

                updateSystem.SendEvent(new WorldTimer.Updates.Event(info), entityId.EntityId);
            });
        }
    }
}
