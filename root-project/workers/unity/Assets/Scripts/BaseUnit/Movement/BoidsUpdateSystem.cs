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
                    ComponentType.ReadOnly<BaseUnitTarget.Component>(),
                    ComponentType.ReadOnly<CommanderStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(BoidComponent.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        float diffMin = 0.1f;
        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref BoidComponent.Component boid,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitTarget.Component target,
                                          ref CommanderStatus.Component commander,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var range = RangeDictionary.GetBoidsRange(commander.Rank);
                var allies = getAllyUnits(status.Side, pos, range, UnitType.Soldier, UnitType.Commander);

                var alliesCount = allies.Count;
                if (alliesCount == 0)
                    return;

                float bufferRate = 1.0f;
                switch(target.State)
                {
                    case TargetState.MovementTarget:    bufferRate = 0.5f;  break;
                    case TargetState.OutOfRange:        bufferRate = 1.0f;  break;
                    case TargetState.ActionTarget:      bufferRate = -0.3f; break;
                }

                var positions = new List<Vector3>();
                var center = pos + trans.forward * boid.ForwardLength;
                var vector = Vector3.zero;

                foreach(var unit in allies) {
                    if (TryGetComponentObject<Transform>(unit.id, out var t) == false)
                        continue;

                    vector += t.forward;
                    positions.Add(unit.pos);
                }

                vector /= alliesCount;

                foreach(var unit in allies) {
                    if (TryGetComponent<BaseUnitMovement.Component>(unit.id, out var movement) == false)
                        continue;
                    
                    var baseVec = movement.Value.BoidVector.Vector.ToUnityVector();
                    var boidVec = Vector3.zero;

                    var inter = RangeDictionary.UnitInter;
                    if (unit.type != UnitType.Commander) {
                        foreach (var p in positions) {
                            var sep = unit.pos - p;

                            boidVec += sep;
                        }

                        boidVec = (boidVec / alliesCount) * boid.SepareteWeight;
                        boidVec += vector * boid.AlignmentWeight;
                        boidVec += (center - unit.pos) * boid.CohesionWeight;
                    }

                    var diff = boidVec - baseVec;
                    if (diff.sqrMagnitude < diffMin * diffMin)
                        continue;

                    var boidVector = new BoidVector(boidVec.ToFixedPointVector3(), bufferRate);
                    this.UpdateSystem.SendEvent(new BaseUnitMovement.BoidDiffed.Event(boidVector), unit.id);
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
