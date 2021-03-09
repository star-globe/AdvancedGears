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
    internal class TimerSynchronizeSystem : SpatialComponentBaseSystem
    {
        EntityQuery group;
        double seconds;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<WorldTimer.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        const int upInter = 10;

        protected override void OnUpdate()
        {
            if (UnityEngine.Random.Range(0,upInter) != 0)
                return;

            this.seconds = TimerUtils.CurrentTime;

            Entities.With(group).ForEach((Entity entity,
                                          ref WorldTimer.Component timer,
                                          ref SpatialEntityId entityId) =>
            {
                if (UnityEngine.Random.Range(0,upInter) != 0)
                    return;

                timer.UtcSeconds = this.seconds;

                var info = new UpdateTimerInfo
                {
                    CurrentSeconds = seconds,
                };

                this.UpdateSystem.SendEvent(new WorldTimer.Updates.Event(info), entityId.EntityId);
            });
        }
    }
}
