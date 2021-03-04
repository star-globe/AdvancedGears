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
    internal class VirtualArmyUpdateSystem : BaseSearchSystem
    {
        private EntityQuerySet commanderQuerySet;
        private EntityQuerySet strongholdQuerySet;

        protected override void OnCreate()
        {
            base.OnCreate();

            commanderQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<CommanderTeam.Component>(),
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 1);

            strongholdQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<TurretHub.Component>(),
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 1);
        }

        protected override void OnUpdate()
        {
            UpdateCommanderTeam();
        }

        private void UpdateCommanderTeam()
        {
            if (CheckTime(ref commanderQuerySet.inter) == false)
                return;

            Entities.With(commanderQuerySet.group).ForEach((Entity entity,
                                                    ref VirtualArmy.Component army,
                                                    ref CommanderTeam.Component team,
                                                    ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var unit = getNearestPlayer(pos, HexDictionary.HexEdgeLength, selfId:null, UnitType.Advanced);
                if (unit == null) {
                    var followers = team.FollowerInfo.Followers;
                    if (army.IsActive && army.SimpleUnits.Count == followers.Count)
                        SyncTroop(army.SimpleUnits, trans);
                    else
                        Virtualize(ref army, trans, team.FollowerInfo.Followers);
                }
                else if (army.IsActive)
                    Realize(ref army, trans);
            });
        }

        private void SyncTroop(Dictionary<EntityId,SimpleUnit> simpleUnits, Transform trans)
        {
            var pos = trans.position;
            var rot = trans.rotation;

            var posDiff = MovementDictionary.SleepPosDiff;
            var rotDiff = MovementDictionary.SleepRotDiff;

            foreach (var kvp in simpleUnits)
            {
                UnitTransform unit = null;
                if (TryGetComponentObject(kvp.Key, out unit) == false)
                    continue;

                var t = unit.transform.parent;
                var buffer = unit.Bounds.extents.y + 0.1f;

                var p = GetGrounded(pos + rot * kvp.Value.RelativePos.ToUnityVector(), buffer);
                var p_diff = p - t.position;
                if (p_diff.sqrMagnitude >= posDiff * posDiff)
                    t.position = p;

                var r = kvp.Value.RelativeRot.ToUnityQuaternion() * rot;
                var r_diff = Vector3.Angle(t.forward, r * Vector3.forward);
                if (r_diff > rotDiff)
                    t.rotation = r;
            }
        }

        private void Virtualize(ref VirtualArmy.Component army, Transform trans, List<EntityId> followers)
        {
            army.IsActive = true;
            var units = army.SimpleUnits;
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

            army.SimpleUnits = units;
            army.AlarmInter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.AlarmInter);
        }

        private void Realize(ref VirtualArmy.Component army, Transform trans)
        {
            army.IsActive = false;
            SyncTroop(army.SimpleUnits, trans);

            foreach (var kvp in army.SimpleUnits)
                SendSleepOrder(kvp.Key, SleepOrderType.WakeUp);

            army.SimpleUnits.Clear();
        }

        private void AlarmUnits(ref VirtualArmy.Component army)
        {
            var inter = army.AlarmInter;
            if (CheckTime(ref inter) == false)
                return;

            army.AlarmInter = inter;
            foreach (var kvp in army.SimpleUnits)
                SendSleepOrders(kvp.Key, SleepOrderType.WakeUp);
        }

        Vector3 GetGrounded(Vector3 pos, float buffer)
        {
            return PhysicsUtils.GetGroundPosition(new Vector3(pos.x, pos.y + 10.0f, pos.z)) + Vector3.up * buffer;
        }

        private void UpdateTurrets()
        {
            if (CheckTime(ref strongholdQuerySet.inter) == false)
                return;

            Entities.With(strongholdQuerySet.group).ForEach((Entity entity,
                                                    ref VirtualArmy.Component army,
                                                    ref TurretHub.Component turret,
                                                    ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var unit = getNearestPlayer(pos, HexDictionary.HexEdgeLength, selfId:null, UnitType.Advanced);
                if (unit == null) {
                    if (army.IsActive == false)
                        VirtualizeTurrests(ref army, turret.TurretsDatas);
                }
                else {
                    if (army.IsActive) 
                        RealizeTurrets(ref army, turret.TurretsDatas);
                    else
                        AlarmTurrets(ref army, turret.TurretsDatas);
                }
            });
        }

        private void VirtualizeTurrests(ref VirtualArmy.Component army, Dictionary<EntityId,TurretInfo> turretsDatas)
        {
            army.IsActive = true;
            SendSleepOrdersToTurret(turretsDatas, SleepOrderType.Sleep);
        }

        private void RealizeTurrets(ref VirtualArmy.Component army, Dictionary<EntityId,TurretInfo> turretsDatas)
        {
            army.IsActive = false;
            army.AlarmInter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.AlarmInter);
            SendSleepOrdersToTurret(turretsDatas, SleepOrderType.WakeUp);
        }

        private void SendSleepOrdersToTurret(Dictionary<EntityId,TurretInfo> turrets, SleepOrderType order)
        {
            foreach (var t in turrets)
                SendSleepOrder(t.Key, order);
        }

        private void AlarmTurrets(ref VirtualArmy.Component army, Dictionary<EntityId,TurretInfo> turrets)
        {
            var inter = army.AlarmInter;
            if (CheckTime(ref inter) == false)
                return;

            army.AlarmInter = inter;
            foreach (var kvp in turrets)
                SendSleepOrders(kvp.Key, SleepOrderType.WakeUp);
        }

        private void SendSleepOrder(EntityId id, SleepOrderType order)
        {
            this.UpdateSystem.SendEvent(new SleepComponent.SleepOrdered.Event(new SleepOrderInfo(order)), id);
        }
    }
}
