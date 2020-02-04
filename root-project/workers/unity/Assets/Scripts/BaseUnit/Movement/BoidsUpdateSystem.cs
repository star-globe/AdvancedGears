using System;
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

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<BoidComponent.Component>(),
                    ComponentType.ReadOnly<BoidComponent.ComponentAuthority>(),
                    ComponentType.ReadOnly<Transform>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<CommanderStatus.Component>()
            );

            group.SetFilter(BoidComponent.ComponentAuthority.Authoritative);
        }

        Ray vertical = new Ray();
        //readonly int layer = //LayerMask.//LayerMask.GetMask("Ground");

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BoidComponent.Component boid,
                                          ref BaseUnitStatus.Component status,
                                          ref CommanderStatus.Component commander) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var range = RangeDictionary.GetBoidsRange(commander.Rank);
                var allies = getAllyUnits(status.Side, pos, range, UnitType.Soldier);

                var mates = new List<Flockmate>();
                foreach(var unit in allies) {
                    if (TryGetComponentObject<Rigidbody>(unit.id, out var rigid) == false)
                        continue;

                    var flock = new Flockmate()
                    {
                        Position = unit.pos.ToWorldPosition(this.Origin),
                        Vector = rigid.velocity.ToFixedPointVector3(),
                    };

                    mates.Add(flock);
                }

                boid.FlockMates = mates;
            });
        }
    }
}
