using System;
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

        readonly Dictionary<EntityId,float> speedDic = new Dictionary<EntityId, float>();

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
                    ComponentType.ReadOnly<BaseUnitMovement.Component>(),
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
                                          ref BaseUnitMovement.Component movement,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var side = status.Side;
                var speed = movement.MoveSpeed;
                var forward = trans.forward;
                var commanderRank = commander.Rank;

                var soldierRange = RangeDictionary.BaseBoidsRange;
                var soldiers = getAllyUnits(side, pos, soldierRange, allowDead:false, selfId:entityId.EntityId, UnitType.Soldier);

                float f_length = boid.ForwardLength;
                boid_calculate(pos + forward * f_length, pos, soldierRange, speed,
                               boid.SepareteWeight, boid.AlignmentWeight, boid.CohesionWeight, soldiers);

                if (commanderRank < 1)
                    return;

                var commanderRange = RangeDictionary.GetBoidsRange(commanderRank);
                var commanders = getAllyUnits(side, pos, commanderRange, allowDead: false, selfId: entityId.EntityId, UnitType.Commander);
                commanders.RemoveAll(unit => TryGetComponent<CommanderStatus.Component>(unit.id, out var com) == false || com.Value.Rank != commanderRank - 1);

                f_length = AttackLogicDictionary.RankScaled(f_length, commanderRank);
                f_length += commander.AllyRange;

                boid_calculate(pos + forward * f_length, pos, commanderRange, speed,
                               boid.SepareteWeight, boid.AlignmentWeight, boid.CohesionWeight, commanders);
            });
        }

        private void boid_calculate(Vector3 center, Vector3 pos, float range, float centerMoveSpeed,
                                   float separeteWeight, float alignmentWeight, float cohesionWeight, List<UnitInfo> allies)
        {
            var rate = range / RangeDictionary.BaseBoidsRange;
            var vector = Vector3.zero;

            speedDic.Clear();
            foreach (var unit in allies)
            {
                if (TryGetComponentObject<Transform>(unit.id, out var t) == false)
                    continue;

                if (TryGetComponent<BaseUnitMovement.Component>(unit.id, out var move) == false)
                    continue;

                speedDic[unit.id] = move.Value.MoveSpeed;

                vector += t.forward;
            }

            vector /= allies.Count;

            foreach (var unit in allies)
            {
                if (TryGetComponent<BaseUnitSight.Component>(unit.id, out var sight) == false)
                    continue;

                var boidVector = sight.Value.BoidVector;
                var baseVec = boidVector.Vector.ToUnityVector();
                var boidVec = Vector3.zero;

                var inter = RangeDictionary.UnitInter;

                var length = Mathf.Max((pos - unit.pos).magnitude, 1.0f);
                var potential = AttackLogicDictionary.BoidPotential(1, length , range);

                if (potential <= boidVector.Potential)
                    continue;

                if (speedDic.TryGetValue(unit.id, out var selfSpeed) == false)
                    continue;

                var scaledLength = length / rate;
                var syncSpeed = (centerMoveSpeed * 1.0f + selfSpeed * scaledLength) / (scaledLength + 1.0f);

                foreach (var other in allies)
                {
                    if (other.id == unit.id)
                        continue;

                    if (speedDic.TryGetValue(other.id, out var speed) == false)
                        continue;

                    var sep = (unit.pos - other.pos) / rate;
                    boidVec += sep;
                }

                boidVec = (boidVec / allies.Count) * separeteWeight;    // todo scaled
                boidVec += vector * alignmentWeight;                    // todo scaled
                boidVec += ((center - unit.pos) / rate) * cohesionWeight;    // todo scaled

                boidVec = boidVec.normalized * syncSpeed * 10.0f;

                var diffVec = boidVec - baseVec;
                var diffCenter = center - sight.Value.BoidVector.Center.ToUnityVector();
                if (diffVec.sqrMagnitude < diffMinVec && diffCenter.sqrMagnitude < diffMinPos)
                    continue;

                boidVector = new BoidVector(boidVec.ToFixedPointVector3(), center.ToWorldPosition(this.Origin), range, potential);
                this.UpdateSystem.SendEvent(new BaseUnitSight.BoidDiffed.Event(boidVector), unit.id);
            }
        }
    }
}
