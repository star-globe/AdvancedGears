using System;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class TimerSynchronizeSystem : SpatialComponentSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

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

                this.UpdateSystem.SendEvent(new WorldTimer.Updates.Event(info), entityId.EntityId);
            });
        }
    }
}
