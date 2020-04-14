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

        //TODO:test to omit "potentialDic" and "speedDic"
        readonly Dictionary<EntityId, float> potentialDic = new Dictionary<EntityId, float>();
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

            potentialDic.Clear();

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

                var range = RangeDictionary.GetBoidsRange(commander.Rank);
                var allies = getAllyUnits(status.Side, pos, range, allowDead: false, UnitType.Soldier, UnitType.Commander);

                var alliesCount = allies.Count;
                if (alliesCount == 0)
                    return;

                //float bufferRate = 1.0f;
                //switch(target.State)
                //{
                //    case TargetState.MovementTarget:    bufferRate = 0.5f;  break;
                //    case TargetState.OutOfRange:        bufferRate = 1.0f;  break;
                //    case TargetState.ActionTarget:      bufferRate = 0.4f; break;
                //}

                var center = pos + trans.forward * boid.ForwardLength;
                var vector = Vector3.zero;

                speedDic.Clear();
                foreach(var unit in allies) {
                    if (TryGetComponentObject<Transform>(unit.id, out var t) == false)
                        continue;

                    //float rate = unit.type != UnitType.Commander ? 1.0f: 2.0f;
                    if (TryGetComponent<BaseUnitMovement.Component>(unit.id, out var move) == false)
                        continue;

                    speedDic[unit.id] = move.Value.MoveSpeed;

                    vector += t.forward;// * rate;
                }

                vector /= alliesCount;

                foreach(var unit in allies) {
                    if (TryGetComponent<BaseUnitSight.Component>(unit.id, out var sight) == false)
                        continue;

                    var boidVector = sight.Value.BoidVector;
                    var baseVec = boidVector.Vector.ToUnityVector();
                    var boidVec = Vector3.zero;

                    var inter = RangeDictionary.UnitInter;
                    if (unit.type == UnitType.Commander)
                        continue;

                    var length = Mathf.Max((pos - unit.pos).magnitude, 1.0f);
                    var potential = AttackLogicDictionary.BoidPotential(1, length, range);

                    float basePotential = boidVector.Potential;
                    if (potentialDic.TryGetValue(unit.id, out var p)) {
                        basePotential = p;
                    }

                    if (potential <= basePotential)
                        continue;

                    potentialDic[unit.id] = p;

                    if (speedDic.TryGetValue(unit.id, out var selfSpeed) == false)
                        continue;

                    var syncSpeed = (movement.MoveSpeed + selfSpeed) / 2;//selfSpeed;
                    var diffSpeed = 0.0f;

                    foreach (var other in allies) {
                        if (other.id == unit.id)
                            continue;

                        if (speedDic.TryGetValue(other.id, out var speed) == false)
                            continue;

                        var sep = unit.pos - other.pos;
                        boidVec += sep;

                        diffSpeed += (speed - selfSpeed) / (1.0f + (sep.magnitude/ (range * range)));
                    }

                    //syncSpeed += diffSpeed / alliesCount;
                    boidVec = (boidVec / alliesCount) * boid.SepareteWeight;
                    boidVec += vector * boid.AlignmentWeight;
                    boidVec += (center - unit.pos) * boid.CohesionWeight;

                    boidVec = boidVec.normalized * syncSpeed * 10.0f;

                    var diffVec = boidVec - baseVec;
                    var diffCenter = center - sight.Value.BoidVector.Center.ToUnityVector();
                    if (diffVec.sqrMagnitude < diffMinVec && diffCenter.sqrMagnitude < diffMinPos)
                        continue;

                    boidVector = new BoidVector(boidVec.ToFixedPointVector3(), center.ToWorldPosition(this.Origin), range, potential);
                    this.UpdateSystem.SendEvent(new BaseUnitSight.BoidDiffed.Event(boidVector), unit.id);
                }
            });
        }

#if false
        readonly Dictionary<uint,List<UnitInfo>> commandersDic = new Dictionary<uint, List<UnitInfo>>();
        readonly List<UnitInfo> soldiers = new List<UnitInfo>();
        // Calc BoidVector for Commander.
        // Do the same process for each rank.
        private void CalcCommandersBoid(Vector3 pos, ref BaseUnitStatus.Component status, ref CommanderStatus.Component commander)
        {
            foreach(var kvp in commandersDic)
                kvp.Value.Clear();

            soldiers.Clear();

            var range = RangeDictionary.GetBoidsRange(commander.Rank);
            var allies = getAllyUnits(status.Side, pos, range, allowDead: false, UnitType.Soldier, UnitType.Commander);

            foreach(var unit in allies) {
                if (unit.type == UnitType.Soldier) {
                    soldiers.Add(unit);
                    continue;
                }

                if (TryGetComponent<CommanderStatus.Component>(unit.id, out var com) == false)
                        continue;

                var rank = com.Value.Rank;
                if (commandersDic.ContainsKey(rank) == false)
                    commandersDic[rank] = new List<UnitInfo>();

                commandersDic[rank].Add(unit);
            }

            foreach(var kvp in commandersDic) {

            }
        }

        //TODO:omit dictionary
        private void BoidCalculate(Vector3 center, float centerMoveSpeed, List<UnitInfo> allies)
        {
            var vector = Vector3.zero;

            speedDic.Clear();
            foreach(var unit in allies) {
                if (TryGetComponentObject<Transform>(unit.id, out var t) == false)
                    continue;

                if (TryGetComponent<BaseUnitMovement.Component>(unit.id, out var move) == false)
                    continue;

                speedDic[unit.id] = move.Value.MoveSpeed;
                vector += t.forward;// * rate;
            }

            vector /= alliesCount;

            foreach(var unit in allies) {
                if (TryGetComponent<BaseUnitSight.Component>(unit.id, out var sight) == false)
                    continue;

                var boidVector = sight.Value.BoidVector;
                var baseVec = boidVector.Vector.ToUnityVector();
                var boidVec = Vector3.zero;

                var length = Mathf.Max((pos - unit.pos).magnitude, 1.0f);
                var potential = AttackLogicDictionary.BoidPotential(1, length, range);

                float basePotential = boidVector.Potential;
                if (potentialDic.TryGetValue(unit.id, out var p)) {
                    basePotential = p;
                }

                if (potential <= basePotential)
                    continue;

                potentialDic[unit.id] = p;

                if (speedDic.TryGetValue(unit.id, out var selfSpeed) == false)
                    continue;

                var syncSpeed = (centerMoveSpeed + selfSpeed) / 2;
                var diffSpeed = 0.0f;

                foreach (var other in allies) {
                    if (other.id == unit.id)
                        continue;

                    if (speedDic.TryGetValue(other.id, out var speed) == false)
                        continue;

                    var sep = unit.pos - other.pos;
                    boidVec += sep;

                    diffSpeed += (speed - selfSpeed) / (1.0f + (sep.magnitude/ (range * range)));
                }

                //syncSpeed += diffSpeed / alliesCount;
                boidVec = (boidVec / alliesCount) * boid.SepareteWeight;
                boidVec += vector * boid.AlignmentWeight;
                boidVec += (center - unit.pos) * boid.CohesionWeight;

                boidVec = boidVec.normalized * syncSpeed * 10.0f;

                var diffVec = boidVec - baseVec;
                var diffCenter = center - sight.Value.BoidVector.Center.ToUnityVector();
                if (diffVec.sqrMagnitude < diffMinVec && diffCenter.sqrMagnitude < diffMinPos)
                    continue;

                boidVector = new BoidVector(boidVec.ToFixedPointVector3(), center.ToWorldPosition(this.Origin), range, potential);
                this.UpdateSystem.SendEvent(new BaseUnitSight.BoidDiffed.Event(boidVector), unit.id);
            }
        }
#endif
    }

    public struct Flockmate
    {
        public Vector3 position;
        public Vector3 vector;
    }
}
