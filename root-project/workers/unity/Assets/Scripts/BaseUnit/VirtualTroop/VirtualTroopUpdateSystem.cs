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
                var unit = getNearestPlayer(pos, range, UnitType.Advanced);

                if ((unit == null) == troop.IsActive)
                    return;

                troop.IsActive = unit == null;

                if (troop.IsActive) {
                    Virtualize(status.Side, pos, boidRange, troop.SimpleUnits);
                }
                else {
                    Realize(pos, troop.SimpleUnits);
                }
            });
        }

        private void Virtualize(UnitSide side, in Vector3 pos, float range, Dictionary<EntityId,SimpleUnit> dic)
        {
            dic.Clear();

            var allies = getAllyUnits(side, pos, range, UnitType.Soldier);
            foreach(var u in allies) {

                //var rePos = u.pos - pos;
                //dic.Add(u.id, new SimpleUnit(rePos.ToFixedPointVector3(),
                                            // ,,));

                //relative_pos = 2;
                //improbable.gdk.transform_synchronization.CompressedQuaternion relative_root = 3;
                //int32 health = 4;
                //int32 attack = 5;
                //float attack_range = 6;
            }
        }

        private void Realize(in Vector3 pos, Dictionary<EntityId,SimpleUnit> dic)
        {
            foreach(var kvp in dic) {

            }
        }
    }
}
