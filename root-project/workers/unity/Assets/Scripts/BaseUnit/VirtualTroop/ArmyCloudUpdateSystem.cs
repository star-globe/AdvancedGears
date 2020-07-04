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
    internal class ArmyCloudUpdateSystem : BaseSearchSystem
    {
        EntityQuery group;

        IntervalChecker inter;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                    ComponentType.ReadWrite<ArmyCloud.Component>(),
                    ComponentType.ReadOnly<ArmyCloud.HasAuthority>(),
                    ComponentType.ReadWrite<Position.Component>(),
                    ComponentType.ReadOnly<Position.HasAuthority>(),
                    ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                    ComponentType.ReadOnly<SpatialEntityId>()
            );

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f);
        }

        const float sightRate = 10.0f;
        protected override void OnUpdate()
        {
            if (CheckTime(ref inter) == false)
                return;

            Entities.With(group).ForEach((Entity entity,
                                          ref ArmyCloud.Component army,
                                          ref Position.Component position,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                var pos = position.Coords.ToUnityVector();
                var containers = army.TroopContainers;

                float range = RangeDictionary.ArmyCloudRange;
                var unit = getNearestPlayer(pos, range, selfId:null, UnitType.Advanced);
                if (unit == null)
                    Virtualize(status.Side, pos, range, containers);
                else
                    Realize(pos, containers);
            });
        }

        private void Virtualize(UnitSide side, Vector3 position, float range, Dictionary<uint,TroopContainer> containers)
        {
            containers.Clear();

            var allies = getAllyUnits(side, position, range, allowDead:false, UnitType.Commander);
            foreach(var u in allies) {
                if (!this.TryGetComponent<BaseUnitHealth.Component>(u.id, out var health) ||
                    !this.TryGetComponent<GunComponent.Component>(u.id, out var gun) ||
                    !this.TryGetComponent<BaseUnitStatus.Component>(u.id, out var status)) {
                    continue;
                }

                var rank = status.Value.Rank;
                if (containers.TryGetValue(rank, out var troop) == false) {
                    troop = new TroopContainer() { Rank = rank };
                }

                var dic = troop.SimpleUnits;
                var simple = new SimpleUnit();
                //var inverse = Quaternion.Inverse(trans.rotation);
                simple.RelativePos = (u.pos - position).ToFixedPointVector3();//(inverse * (u.pos - trans.position)).ToFixedPointVector3();
                simple.RelativeRot = u.rot.ToCompressedQuaternion();
                simple.Health = health == null ? 0: health.Value.Health;
                // todo calc attack and range from GunComponent;

                //int32 attack = 5;
                //float attack_range = 6;
                dic.Add(u.id, simple);

                troop.SimpleUnits = dic;
                containers[rank] = troop;
            }
        }

        private void Realize(Vector3 pos, Dictionary<uint,TroopContainer> containers)
        {
            foreach (var con in containers) {
                foreach(var kvp in con.Value.SimpleUnits) {
                    var id = kvp.Key;
                    if (this.TryGetComponentObject<Transform>(id, out var t)) {
                        t.position = pos + kvp.Value.RelativePos.ToUnityVector();
                        t.rotation = kvp.Value.RelativeRot.ToUnityQuaternion();
                    }

                    if (this.TryGetComponent<BaseUnitHealth.Component>(id, out var health)) {
                        var diff = kvp.Value.Health - health.Value.Health;
                        this.UpdateSystem.SendEvent(new BaseUnitHealth.HealthDiffed.Event(new HealthDiff { Diff = diff }), id);
                    }
                }
            }
        }
    }
}
