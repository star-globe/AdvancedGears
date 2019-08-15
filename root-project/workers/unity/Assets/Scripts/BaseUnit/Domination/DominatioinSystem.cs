using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class DominationSystem : BaseSearchSystem
    {
        EntityQuery group;
        CommandSystem commandSystem;
        ComponentUpdateSystem updateSystem;
        ILogDispatcher logDispatcher;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            var worker = World.GetExistingSystem<WorkerSystem>();
            origin = worker.Origin;
            logDispatcher = worker.LogDispatcher;

            commandSystem = World.GetExistingSystem<CommandSystem>();
            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();
            group = GetEntityQuery(
                ComponentType.ReadWrite<DominationStamina.Component>(),
                ComponentType.ReadOnly<DominationStamina.ComponentAuthority>(),
                ComponentType.ReadOnly<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(DominationStamina.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleCaputuring();
        }

        void HandleCaputuring()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref DominationStamina.Component domination,
                                          ref FuelComponent.Component fuel,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Dead)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var time = Time.time;
                var inter = domination.Interval;
                float diff;
                if (inter.CheckTime(time, out diff) == false)
                    return;

                domination.Interval = inter;

                float range = domination.Range;
                var f_comp = fuel;

                //
            });
        }
    }
}
