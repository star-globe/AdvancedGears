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
        long ticks;

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

            this.ticks = DateTime.UtcNow.Ticks;

            Entities.With(group).ForEach((Entity entity,
                                          ref WorldTimer.Component timer,
                                          ref SpatialEntityId entityId) =>
            {
                if (UnityEngine.Random.Range(0,upInter) != 0)
                    return;

                timer.utc_ticks = this.ticks;

                var info = new UpdateTimerInfo
                {
                    current_ticks = ticks,
                };

                this.UpdateSystem.SendEvent(new WorldTimer.Updates.Event(info), entityId.EntityId);
            });
        }
    }
}
