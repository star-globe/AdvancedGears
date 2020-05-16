using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class MapQueryServerSystem : MapQueryBaseSystem
    {
        protected override Vector3? BasePosition => this.WorkerSystem.Origin;
        protected override float SearchRadius => 1000.0f;
        protected override IEnumerable<uint> ComponentIds
        {
            get
            {
                yield return ArmyCloud.ComponentId;
            }
        }
    }

    [DisableAutoCreation]
    [AlwaysUpdateSystemAttribute]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class MapQueryClientSystem : MapQueryBaseSystem
    {
        Vector3? playerPosition = null;
        protected override Vector3? BasePosition => playerPosition;
        protected override float SearchRadius => 500.0f;
        protected override IEnumerable<uint> ComponentIds
        {
            get
            {
                yield return ArmyCloud.ComponentId;
                yield return 0;//todo symboloc tower;
            }
        }

        IntervalChecker inter = IntervalCheckerInitializer.InitializedChecker(3.0f, setChecked:true);
        private Unity.Entities.EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<PlayerInfo.Component>(),
                ComponentType.ReadOnly<Position.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref inter))
            {
                Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                              ref PlayerInfo.Component playerInfo,
                                              ref Position.Component position) =>
                {
                    if (playerInfo.ClientWorkerId.Equals(this.WorkerSystem.WorkerId) == false)
                        return;

                    playerPosition = position.Coords.ToUnityVector() + this.Origin;
                });
            }

            base.OnUpdate();
        }
    }

    public abstract class MapQueryBaseSystem : EntityQuerySystem
    {
        protected abstract Vector3? BasePosition { get; }
        protected abstract float SearchRadius { get; }
        protected abstract IEnumerable<uint> ComponentIds { get; }


        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            if (this.BasePosition == null)
                return;

            base.OnUpdate();
        }

        protected override void SendEntityQuery()
        {
            base.SendEntityQuery();

            Debug.LogFormat("SendMapQuery. WorkerId:{0}", this.WorkerSystem.WorkerId);
        }

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                var pos = BasePosition != null ? BasePosition.Value: Vector3.zero;

                var list = new List<IConstraint>();
                list.Add(new SphereConstraint(BasePosition.Value.x, BasePosition.Value.y, BasePosition.Value.z, SearchRadius));
                foreach(var id in ComponentIds)
                    list.Add(new ComponentConstraint(id));

                return new ImprobableEntityQuery()
                {
                    Constraint = new AndConstraint(list.ToArray()),
                    ResultType = new SnapshotResultType()
                };
            }
        }
    }
}


