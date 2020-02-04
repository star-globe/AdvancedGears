using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BoidsUpdateSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<BoidComponent.Component>(),
                    ComponentType.ReadOnly<BoidComponent.ComponentAuthority>(),
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<CommanderStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(BoidComponent.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(0.5f);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref BoidComponent.Component boid,
                                          ref BaseUnitStatus.Component status,
                                          ref CommanderStatus.Component commander,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var range = RangeDictionary.GetBoidsRange(commander.Rank);
                var allies = getAllyUnits(status.Side, pos, range, UnitType.Soldier);

                var alliesCount = allies.Count;
                if (alliesCount == 0)
                    return;

                var positions = new List<Vector3>();
                var center = Vector3.zero;
                var vector = Vector3.zero;

                foreach(var unit in allies) {
                    if (TryGetComponentObject<Rigidbody>(unit.id, out var rigid) == false)
                        continue;

                    center += unit.pos;
                    vector += rigid.velocity.normalized;

                    positions.Add(unit.pos);
                }

                center /= alliesCount;
                vector /= alliesCount;

                foreach(var unit in allies) {
                    var boidVec = Vector3.zero;

                    if (unit.id != entityId.EntityId) {
                        foreach (var p in positions)
                            boidVec += (p - unit.pos).normalized * boid.SepareteWeight;

                        boidVec /= alliesCount;
                        boidVec += vector * boid.AlignmentWeight;
                        boidVec += (center - unit.pos).normalized * boid.CohesionWeight;
                    }

                    this.UpdateSystem.SendEvent(new BaseUnitMovement.BoidDiffed.Event(new BoidVector(boidVec.ToFixedPointVector3())), unit.id);
                }
            });
        }
    }

    public struct Flockmate
    {
        public Vector3 position;
        public Vector3 vector;
    }
}
