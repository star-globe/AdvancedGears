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
    internal class ArmyCloudUpdateSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<ArmyCloud.Component>(),
                    ComponentType.ReadOnly<ArmyCloud.ComponentAuthority>(),
                    ComponentType.ReadWrite<Position.Component>(),
                    ComponentType.ReadOnly<Position.ComponentAuthority>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(ArmyCloud.ComponentAuthority.Authoritative);
            group.SetFilter(Position.ComponentAuthority.Authoritative);
            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        const float sightRate = 10.0f;
        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref ArmyCloud.Component army,
                                          ref Position.Component position,
                                          ref SpatialEntityId entityId) =>
            {
                var pos = position.Coords.ToUnityVector();

            });
        }

        private void Virtualize(UnitSide side, Vector3 position, float range, Dictionary<EntityId,SimpleUnit> dic)
        {
            dic.Clear();

            var allies = getAllyUnits(side, position, range, allowDead:false, UnitType.Commander);
            foreach(var u in allies) {
                this.TryGetComponent<BaseUnitHealth.Component>(u.id, out var health);
                this.TryGetComponent<GunComponent.Component>(u.id, out var gun);

                var simple = new SimpleUnit();
                //var inverse = Quaternion.Inverse(trans.rotation);
                simple.RelativePos = (u.pos - position).ToFixedPointVector3();//(inverse * (u.pos - trans.position)).ToFixedPointVector3();
                simple.RelativeRot = u.rot.ToCompressedQuaternion();
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
