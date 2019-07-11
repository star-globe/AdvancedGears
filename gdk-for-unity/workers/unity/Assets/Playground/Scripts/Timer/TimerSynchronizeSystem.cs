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

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                    ComponentType.ReadWrite<WorldTimer.Component>(),
                    ComponentType.ReadOnly<Transform>(),
            );
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref WorldTimer.Component timer,
                                          Transform trans) =>
            {

            });
        }
    }
}
