using System;
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
    internal class WakerUpdateSystem : BaseSearchSystem
    {
        private EntityQuerySet commanderQuerySet;
        private EntityQuerySet strongholdQuerySet;

        protected override void OnCreate()
        {
            base.OnCreate();

            commanderQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<CommanderTeam.Component>();
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 2);

            strongholdQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<TurretHub.Component>();
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 2);
        }

        protected override void OnUpdate()
        {
            UpdateCommanderTeam();
        }

        private void UpdateCommanderTeam();
        {
            if (CheckTime(ref commanderQuerySet.inter) == false)
                return;

            Entities.With(commanderQuerySet.group).ForEach((Entity entity,
                                                    ref VirtualArmy.Component virtual,
                                                    ref CommanderTeam.Component team,
                                                    ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var unit = getNearestPlayer(pos, HexDictionary.EdgeLength, selfId:null, UnitType.Advanced);
                if (unit == null) {
                    if (virtual.IsActive)
                        SyncTroop(virtual.SimpleUnits, trans);
                    else
                        Virtualize(ref virtual, trans, team.FollowerInfo.Followers);
                }
                else if (virtual.IsActive)
                    Realize(ref virtual, trans);
            });
        }

        private void SyncTroop(Dictionary<EntityId,SimpleUnit> simpleUnits, Transform trans)
        {
            var pos = trans.position;
            var rot = trans.rotation;
            foreach (var kvp in simpleUnits)
            {
                Transform t = null;
                if (TryGetComponentObject(kvp.Key, out t) == false)
                    continue;

                var p = GetGrounded(pos + rot * kvp.Value.RelativePos.ToUnityVector());
                var diff = p - t.position;
                if ()
                t.position = GetGrounded(pos + rot * kvp.Value.RelativePos.ToUnityVector());
                t.rotation = kvp.Value.RelativeRot.ToUnityQuaternion() * rot;
            }
        }

        private void Virtualize(ref VirtualArmy.Component virtual, Transform trans, List<EntityId> followers)
        {
            virtual.IsActive = true;
            var units = virtual.SimpleUnits;
            units.Clear();

            var inverse = Quaternion.Inverse(trans.rotation);
            var pos = trans.position;
            foreach (var id in followers) {
                Transform t = null;
                if (TryGetComponentObject(id, out t) == false)
                    continue;

                var simple = new SimpleUnit();

                simple.RelativePos = (inverse * (t.position - pos)).ToFixedPointVector3();
                simple.RelativeRot = (t.rotation * inverse).ToCompressedQuaternion();

                units.Add(id, simple);

                SendSleepOrder(id, SleepOrderType.Sleep);
            }

            virtual.SimpleUnits = units;
        }

        private void Realize(ref VirtualArmy.Component virtual, Transform trans)
        {
            virtual.IsActive = false;
            SyncTroop(virtual.SimpleUnits, trans);

            foreach (var kvp in virtual.SimpleUnits)
                SendSleepOrder(kvp.Key, SleepOrderType.WakeUp);

            virtual.SimpleUnits.Clear();
        }

        Vector3 GetGrounded(Vector3 pos)
        {
            return PhysicsUtils.GetGroundPosition(new Vector3(pos.x, 1000.0f, pos.z)) + Vector3.up * buffer;
        }

        private void UpdateTurrets();
        {
            if (CheckTime(ref strongholdQuerySet.inter) == false)
                return;

            Entities.With(strongholdQuerySet.group).ForEach((Entity entity,
                                                    ref VirtualArmy.Component virtual,
                                                    ref TurretHub.Component turret,
                                                    ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var unit = getNearestPlayer(pos, HexDictionary.EdgeLength, selfId:null, UnitType.Advanced);
                if (unit == null) {
                    if (virtual.IsActive == false)
                        SendSleepOrdersToTurret(turret.TurretsDatas, SleepOrderType.Sleep);
                }
                else if (virtual.IsActive)
                    SendSleepOrdersToTurret(turret.TurretsDatas, SleepOrderType.WakeUp);
            });
        }

        private void SendSleepOrdersToTurret(Dictionary<EntityId,TurretInfo> turrets, SleepOrderType order)
        {
            foreach (var t in turrets)
                SendSleepOrder(t.Key, order);
        }

        private void SendSleepOrder(EntityId id, SleepOrderType order)
        {
            this.UpdateSystem.SendEvent(new SleepComponent.sleep_ordered.Event(new SleepOrderInfo(order)), id);
        }
    }
}
