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
    internal class VirtualTroopUpdateSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<VirtualTroop.Component>(),
                    ComponentType.ReadOnly<VirtualTroop.ComponentAuthority>(),
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<CommanderStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(VirtualTroop.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        const float sightRate = 10.0f;
        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref VirtualTroop.Component troop,
                                          ref BaseUnitStatus.Component status,
                                          ref CommanderStatus.Component commander,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var boidRange = RangeDictionary.GetBoidsRange(commander.Rank);
                var range = boidRange * sightRate;
                var unit = getNearestPlayer(pos, range, selfId:null, UnitType.Advanced);

                if ((unit == null) == troop.IsActive)
                    return;

                troop.IsActive = unit == null;

                if (troop.IsActive) {
                    Virtualize(status.Side, trans, boidRange, troop.SimpleUnits);
                }
                else {
                    Realize(trans, troop.SimpleUnits);
                }
            });
        }

        private void Virtualize(UnitSide side, Transform trans, float range, Dictionary<EntityId,SimpleUnit> dic)
        {
            dic.Clear();

            var allies = getAllyUnits(side, trans.position, range, allowDead:false, UnitType.Soldier);
            foreach(var u in allies) {
                this.TryGetComponent<BaseUnitHealth.Component>(u.id, out var health);
                this.TryGetComponent<GunComponent.Component>(u.id, out var gun);

                var simple = new SimpleUnit();
                var inverse = Quaternion.Inverse(trans.rotation);
                simple.RelativePos = (inverse * (u.pos - trans.position)).ToFixedPointVector3();
                simple.RelativeRot = (u.rot * inverse).ToCompressedQuaternion();
                simple.Health = health == null ? 0: health.Value.Health;
                // todo calc attack and range from GunComponent;

                //int32 attack = 5;
                //float attack_range = 6;
                dic.Add(u.id, simple);
            }
        }

        private void Realize(Transform trans, Dictionary<EntityId,SimpleUnit> dic)
        {
            var pos = trans.position;
            var rot = trans.rotation;
            foreach(var kvp in dic) {
                var id = kvp.Key;
                if (this.TryGetComponentObject<Transform>(id, out var t)) {
                    t.position = trans.position + rot * kvp.Value.RelativePos.ToUnityVector();
                    t.rotation = kvp.Value.RelativeRot.ToUnityQuaternion() * rot;
                }

                if (this.TryGetComponent<BaseUnitHealth.Component>(id, out var health)) {
                    var diff = kvp.Value.Health - health.Value.Health;
                    this.UpdateSystem.SendEvent(new BaseUnitHealth.HealthDiffed.Event(new HealthDiff { Diff = diff }), id);
                }
            }
        }
    }
}
