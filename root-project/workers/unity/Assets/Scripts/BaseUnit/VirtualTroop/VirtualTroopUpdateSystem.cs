using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class VirtualTroopUpdateSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<VirtualTroop.Component>(),
                    ComponentType.ReadOnly<VirtualTroop.HasAuthority>(),
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        class TroopDamage
        {
            public EntityId id;
            public int healthDiff;
        }

        const float sightRate = 10.0f;
        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            var damageDic = new Dictionary<EntityId, TroopDamage>();

            Entities.With(group).ForEach((Entity entity,
                                          ref VirtualTroop.Component troop,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var boidRange = RangeDictionary.GetBoidsRange(status.Rank);
                var range = boidRange * sightRate;
                var unit = getNearestPlayer(pos, range, selfId:null, GetSingleUnitTypes(UnitType.Advanced));

                if ((unit == null) == troop.IsActive) {
                    if (CheckConflict(ref troop, status.Side, trans, status.Rank, out var damage))
                        damageDic[damage.id] = damage;
                }
                else {
                    UpdateContainer(ref troop, unit == null, status.Side, trans, boidRange, status.Rank);
                }
            });

            // SendDamage
            foreach(var kvp in damageDic) {
                var diff = new TotalHealthDiff(kvp.Value.healthDiff);
                this.UpdateSystem.SendEvent(new VirtualTroop.TotalHealthDiff.Event(diff), kvp.Key);
            }
        }

        const float buffer = 0.3f;
        private bool CheckConflict(ref VirtualTroop.Component troop, UnitSide side, Transform trans, uint rank, out TroopDamage damage)
        {
            damage = null;
            if (troop.IsActive == false)
                return false;

            var atkInter = troop.AttackInter;
            if (CheckTime(ref atkInter) == false)
                return false;

            troop.AttackInter = atkInter;
            var container = troop.TroopContainer;
            float range = 0;
            float attack = 0;
            foreach(var sm in container.SimpleUnits) {
                range += sm.Value.AttackRange;
                attack += sm.Value.Attack * UnityEngine.Random.Range(1.0f - buffer, 1.0f + buffer);
            }

            if (attack == 0)
                return false;

            var count = container.SimpleUnits.Count;
            if (count > 0) {
                range /= count;
            }

            float sqrtlength = float.MaxValue;
            var pos = trans.position;
            var units = getEnemyUnits(side, pos, range, allowDead:false, GetSingleUnitTypes(UnitType.Commander));
            foreach(var u in units) {
                if (this.TryGetComponent<VirtualTroop.Component>(u.id, out var tp) == false)
                    continue;

                if (u.rank != rank)
                    continue;

                if (tp.Value.IsActive == false)
                    continue;

                var diff = (u.pos - pos).sqrMagnitude;
                if (diff >= sqrtlength)
                    continue;

                sqrtlength = diff;
                damage = damage ?? new TroopDamage();
                damage.id = u.id;
                damage.healthDiff = (int)attack;
            }

            return damage != null;
        }

        private void UpdateContainer(ref VirtualTroop.Component troop, bool isActive, UnitSide side, Transform trans, float range, uint rank)
        {
            troop.IsActive = isActive;
            var container = troop.TroopContainer;

            if (troop.IsActive) {
                Virtualize(side, trans, range, container.SimpleUnits);
            }
            else {
                Realize(side, trans, container.SimpleUnits);
            }

            container.Rank = rank;
            troop.TroopContainer = container;
        }

        private void Virtualize(UnitSide side, Transform trans, float range, Dictionary<EntityId,SimpleUnit> dic)
        {
            dic.Clear();

            var allies = getAllyUnits(side, trans.position, range, allowDead:false, GetSingleUnitTypes(UnitType.Soldier));
            foreach(var u in allies) {
                if (!this.TryGetComponent<BaseUnitHealth.Component>(u.id, out var health) ||
                    !this.TryGetComponent<GunComponent.Component>(u.id, out var gun))
                    continue;

                var simple = new SimpleUnit();
                var inverse = Quaternion.Inverse(trans.rotation);
                simple.RelativePos = (inverse * (u.pos - trans.position)).ToFixedPointVector3();
                simple.RelativeRot = (u.rot * inverse).ToCompressedQuaternion();
                simple.Health = health == null ? 0: health.Value.Health;
                // todo calc attack and range from GunComponent;

                //int32 attack = 5;
                //float attack_range = 6;
                dic.Add(u.id, simple);

                this.UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(new ForceStateChange(side, UnitState.Sleep)), u.id);
            }
        }

        private void Realize(UnitSide side, Transform trans, Dictionary<EntityId,SimpleUnit> dic)
        {
            var pos = trans.position;
            var rot = trans.rotation;
            foreach(var kvp in dic) {
                var id = kvp.Key;
                if (!this.TryGetComponentObject<Transform>(id, out var t) ||
                    !this.TryGetComponent<BaseUnitHealth.Component>(id, out var health))
                    continue;
                
                t.position = GetGrounded(trans.position + rot * kvp.Value.RelativePos.ToUnityVector());
                t.rotation = kvp.Value.RelativeRot.ToUnityQuaternion() * rot;
                
                var diff = kvp.Value.Health - health.Value.Health;
                this.UpdateSystem.SendEvent(new BaseUnitHealth.HealthDiffed.Event(new HealthDiff { Diff = diff }), id);
                
                var state = health.Value.Health > 0 ? UnitState.Alive: UnitState.Dead;

                this.UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(new ForceStateChange(side, state)), id);
            }

            dic.Clear();
        }

        Vector3 GetGrounded(Vector3 pos)
        {
            return PhysicsUtils.GetGroundPosition(new Vector3(pos.x, 1000.0f, pos.z)) + Vector3.up * buffer;
        }
    }
}
