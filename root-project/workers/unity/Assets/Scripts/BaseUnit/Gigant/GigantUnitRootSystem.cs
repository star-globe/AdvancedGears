using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Linq;
using System.Collections.Generic;
using Improbable.Worker.CInterop.Query;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class GigantUnitRootSystem : EntityQuerySystem
    {
        IConstraint[] constraints = null;
        ImprobableEntityQuery query;

        EntityQuerySet querySet;
        const float inter = 1.0f;

        EntityQueryBuilder.F_EDDD<GigantComponent.Component, BaseUnitStatus.Component, SpatialEntityId> action;

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                if (constraints == null)
                {
                    constraints = new IConstraint[] { new ComponentConstraint(HeadQuarters.ComponentId),
                                                      new ComponentConstraint(HexPowerResource.ComponentId)};

                    query = new ImprobableEntityQuery()
                    {
                        Constraint = new AndConstraint(constraints),
                        ResultType = new SnapshotResultType()
                    };
                }

                return query;
            }
        }

        protected override bool IsCheckTime => false;

        protected override void OnCreate()
        {
            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadWrite<GigantComponent.Component>(),
                                            ComponentType.ReadOnly<GigantComponent.HasAuthority>(),
                                            ComponentType.ReadOnly<Transform>(),
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<SpatialEntityId>()),
                                            inter);

            action = Query;
        }

        const float checkRange = 10;

        private void Query(Entity entity,
                      ref GigantComponent.Component gigant,
                      ref BaseUnitStatus.Component status,
                      ref SpatialEntityId eneityId)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Side == UnitSide.None)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            var pos = trans.position;

            if (gigant.RootIndex < 0)
            {
                gigant.RootIndex = 0;

                var coord = pos.ToWorldCoordinates(this.Origin);
                var roots = new List<Coordinates>();

                foreach (var kvp in powerMaps)
                    roots.Add(kvp.Key);

                roots.OrderBy(c => (c - coord).SqrMagnitude());

                foreach (var kvp in hqMaps)
                    roots.Add(kvp.Key);
            }
            else
            {
                var index = gigant.RootIndex;
                var tgt = gigant.Roots[index].ToWorkerPosition(this.Origin);

                // index check
                if ((tgt - pos).sqrMagnitude > checkRange * checkRange)
                    return;

                if (index < gigant.Roots.Count - 1)
                {
                    index++;
                    gigant.RootIndex = index;
                    tgt = gigant.Roots[index].ToWorkerPosition(this.Origin);
                }
                else
                {
                    gigant.RootIndex = -1;
                }
            }
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        protected override void ReceiveSnapshots(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            UnityEngine.Profiling.Profiler.BeginSample("ReceiveSnapshots");

            if (shots.Count > 0)
            {
                SetReciever(shots);
            }
            else
            {
                ClearReciever();
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private readonly Dictionary<Coordinates, EntityId> hqMaps = new Dictionary<Coordinates, EntityId>();
        private readonly Dictionary<Coordinates, EntityId> powerMaps = new Dictionary<Coordinates, EntityId>();

        private void SetReciever(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            hqMaps.Clear();
            powerMaps.Clear();

            foreach (var kvp in shots)
            {
                var id = kvp.Key;
                foreach (var shot in kvp.Value)
                {
                    Position.Snapshot position;
                    if (shot.TryGetComponentSnapshot(out position) == false)
                        continue;

                    if (shot.TryGetComponentSnapshot<HeadQuarters.Snapshot>(out var hq))
                    {
                        hqMaps.Add(position.Coords, id);
                    }
                    else if (shot.TryGetComponentSnapshot<HexPowerResource.Snapshot>(out var power))
                    {
                        powerMaps.Add(position.Coords, id);
                    }
                }
            }
        }

        private void ClearReciever()
        {
            hqMaps.Clear();
            powerMaps.Clear();
        }
    }
}
