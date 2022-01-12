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
    public class ArtilleryTargetSearchSystem : EntityQuerySystem
    {
        IConstraint[] constraints = null;
        ImprobableEntityQuery query;

        EntityQuerySet querySet;
        const float inter = 1.0f;

        EntityQueryBuilder.F_EDDD<UnitActionData, BaseUnitStatus.Component, BaseUnitTarget.Component> action;

        protected override ImprobableEntityQuery EntityQuery
        {
            get
            {
                if (constraints == null)
                {
                    constraints = new IConstraint[] { new ComponentConstraint(StrategyFlare.ComponentId),
                                                      new ComponentConstraint(HeadQuarters.ComponentId) };

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
                                            ComponentType.ReadWrite<BaseUnitTarget.Component>(),
                                            ComponentType.ReadOnly<BaseUnitTarget.HasAuthority>(),
                                            ComponentType.ReadWrite<UnitActionData>(),
                                            ComponentType.ReadOnly<Transform>(),
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<LongRangeBulletComponent.Component>()),
                                            inter);

            action = Query;
        }

        private void Query(Entity entity,
                      ref UnitActionData action,
                      ref BaseUnitStatus.Component status,
                      ref BaseUnitTarget.Component target)
        {
            if (status.State != UnitState.Alive)
                return;

            if (status.Side == UnitSide.None)
                return;

            if (UnitUtils.IsOffensive(status.Type) == false)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            var pos = trans.position;
            var length = action.AttackRange * action.AttackRange;

            Vector3? targetPos = null;

            foreach (var kvp in flareMaps) {
                var p = kvp.Key.ToWorkerPosition(this.Origin);

                var mag = (p - pos).sqrMagnitude;
                if (mag >= length)
                    continue;

                length = mag;
                targetPos = pos;
            }

            action.TargetPosition = targetPos;
            target.State = targetPos == null ? TargetState.None: TargetState.ActionTarget;
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

        private readonly Dictionary<Coordinates, EntityId> flareMaps = new Dictionary<Coordinates, EntityId>();

        private void SetReciever(Dictionary<EntityId, List<EntitySnapshot>> shots)
        {
            flareMaps.Clear();

            foreach (var kvp in shots)
            {
                var id = kvp.Key;
                foreach (var shot in kvp.Value)
                {
                    Position.Snapshot position;
                    if (shot.TryGetComponentSnapshot(out position) == false)
                        continue;

                    flareMaps.Add(position.Coords, id);
                }
            }
        }

        private void ClearReciever()
        {
            flareMaps.Clear();
        }
    }
}
