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

        const int period = 10;
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
            inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        const float diffMinVec = 0.1f * 0.1f;
        const float diffMinPos = 1.0f * 1.0f;
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
                var allies = getAllyUnits(status.Side, pos, range, allowDead: false, UnitType.Soldier, UnitType.Commander);

                var alliesCount = allies.Count;
                if (alliesCount == 0)
                    return;

                float bufferRate = 1.0f;
                switch(target.State)
                {
                    case TargetState.MovementTarget:    bufferRate = 0.5f;  break;
                    case TargetState.OutOfRange:        bufferRate = 1.0f;  break;
                    case TargetState.ActionTarget:      bufferRate = 0.4f; break;
                }

                var center = pos + trans.forward * boid.ForwardLength;
                var vector = Vector3.zero;

                foreach(var unit in allies) {
                    if (TryGetComponentObject<Transform>(unit.id, out var t) == false)
                        continue;

                    //float rate = unit.type != UnitType.Commander ? 1.0f: 2.0f;

                    vector += t.forward;// * rate;
                }

                vector /= alliesCount;

                foreach(var unit in allies) {
                    if (TryGetComponent<BaseUnitSight.Component>(unit.id, out var sight) == false)
                        continue;
                    
                    var baseVec = sight.Value.BoidVector.Vector.ToUnityVector();
                    var boidVec = Vector3.zero;
                    var rate = 1.0f;

                    var inter = RangeDictionary.UnitInter;
                    if (unit.type == UnitType.Commander)
                        continue;
                    else {
                        foreach (var other in allies)
                        {
                            if (other.id == unit.id)
                                continue;

                            var sep = unit.pos - other.pos;
                            //sep *= other.type != UnitType.Commander ? 1.0f: 3.0f; 

                            boidVec += sep;
                        }

                        boidVec = (boidVec / alliesCount) * boid.SepareteWeight;
                        boidVec += vector * boid.AlignmentWeight;
                        boidVec += (center - unit.pos) * boid.CohesionWeight;

                        rate = bufferRate * (center - unit.pos).sqrMagnitude / 100.0f;
                    }

                    var diffVec = boidVec - baseVec;
                    var diffCenter = center - sight.Value.BoidVector.Center.ToUnityVector();
                    if (diffVec.sqrMagnitude < diffMinVec && diffCenter.sqrMagnitude < diffMinPos)
                        continue;

                    var boidVector = new BoidVector(boidVec.ToFixedPointVector3(), center.ToWorldPosition(this.Origin), rate, range);
                    this.UpdateSystem.SendEvent(new BaseUnitSight.BoidDiffed.Event(boidVector), unit.id);
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
