using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections;
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
    internal class LongRangeBulletUpdateSystem : EntityQuerySystem
    {
        IConstraint[] constraints = null;
        ImprobableEntityQuery query;
        private EntityQueryBuilder.F_EDDD<LongRangeBulletComponent.Component, Position.Component, SpatialEntityId> action;

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                if (constraints == null) {
                    constraints = new IConstraint[] { new ComponentConstraint(StrategyLongBulletReceiver.ComponentId) };

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

        EntityQuerySet querySet;
        const float inter = 1.0f;

        protected override void OnCreate()
        {
            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadOnly<LongRangeBulletComponent.HasAuthority>(),
                                          ComponentType.ReadWrite<LongRangeBulletComponent.Component>(),
                                          ComponentType.ReadOnly<Position>(),
                                          ComponentType.ReadOnly<SpatialEntityId>()),
                                          inter);

            action = Query;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        private void Query(Entity entity,
                        ref LongRangeBulletComponent.Component bullet,
                        ref Position.Component position,
                        ref SpatialEntityId entityId)
        {
            var pos = position.Coords;

            var length = double.MaxValue;
            EntityId id = new EntityId(-1);

            foreach (var kvp in receiverMaps)
            {
                var diff = pos - kvp.Key;
                var mag = diff.SqrMagnitude();
                if (mag < length) {
                    length = mag;
                    id = kvp.Value;
                }
            }

            bullet.ReceiverId = id;
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

        private readonly Dictionary<Coordinates, EntityId> receiverMaps = new Dictionary<Coordinates, EntityId>();

        private void SetReciever(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            receiverMaps.Clear();

            foreach (var kvp in shots)
            {
                var id = kvp.Key;
                foreach (var shot in kvp.Value)
                {
                    Position.Snapshot position;
                    if (shot.TryGetComponentSnapshot(out position) == false)
                        continue;

                    receiverMaps.Add(position.Coords, id);
                }
            }
        }

        private void ClearReciever()
        {
            receiverMaps.Clear();
        }
    }
}
