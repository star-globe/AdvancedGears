using System.Collections.Generic;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class VirtualArmyUpdateSystem : BaseSearchSystem
    {
        private EntityQuerySet commanderQuerySet;
        private EntityQuerySet turretQuerySet;
        EntityQueryBuilder.F_EDDD<VirtualArmy.Component, CommanderTeam.Component, BaseUnitStatus.Component> commanderAction;
        EntityQueryBuilder.F_EDDD<VirtualArmy.Component, TurretHub.Component, BaseUnitStatus.Component> turretAction;

        protected override void OnCreate()
        {
            base.OnCreate();

            commanderQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<CommanderTeam.Component>(),
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 1);

            turretQuerySet = new EntityQuerySet(GetEntityQuery(
                                                    ComponentType.ReadWrite<VirtualArmy.Component>(),
                                                    ComponentType.ReadOnly<VirtualArmy.HasAuthority>(),
                                                    ComponentType.ReadOnly<TurretHub.Component>(),
                                                    ComponentType.ReadOnly<BaseUnitStatus.Component>()), 1);

            commanderAction = CommanderQuery;
            turretAction = TurretQuery;
        }

        protected override void OnUpdate()
        {
            UnityEngine.Profiling.Profiler.BeginSample("UpdateCommanderTeam");
            UpdateCommanderTeam();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("UpdateTurrets");
            UpdateTurrets();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        #region CommanderTeam
        private void UpdateCommanderTeam()
        {
            if (CheckTime(ref commanderQuerySet.inter) == false)
                return;

            UpdatePlayerPosition();
            Entities.With(commanderQuerySet.group).ForEach(commanderAction);
        }

        private void CommanderQuery(Entity entity,
                                    ref VirtualArmy.Component army,
                                    ref CommanderTeam.Component team,
                                    ref BaseUnitStatus.Component status)
        {
            if (status.State != UnitState.Alive)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            var pos = trans.position;

            var unit = getNearestPlayer(pos, HexDictionary.HexEdgeLength, selfId:null);
            if (unit == null) {
                var followers = team.FollowerInfo.Followers;
                if (army.IsActive && army.SimpleUnits.Count == followers.Count)
                    SyncTroop(army.SimpleUnits, trans);
                else
                    VirtualizeUnits(ref army, trans, team.FollowerInfo.Followers);
            }
            else {
                if (army.IsActive)
                    RealizeUnits(ref army, trans);
                else
                    AlarmUnits(ref army, team.FollowerInfo.Followers);
            }
        }

        private void SyncTroop(Dictionary<EntityId,SimpleUnit> simpleUnits, Transform trans)
        {
            UnityEngine.Profiling.Profiler.BeginSample("SyncTroop");
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
                var buffer = unit.BufferVector.magnitude;

                var p = GetGrounded(pos + rot * kvp.Value.RelativePos.ToUnityVector(), buffer);
                var p_diff = p - t.position;
                if (p_diff.sqrMagnitude >= posDiff * posDiff)
                    t.position = NavMeshUtils.GetNavPoint(t.position, p, unit.Bounds.size.magnitude, WalkableNavArea);

                var r = kvp.Value.RelativeRot.ToUnityQuaternion() * rot;
                var r_diff = Vector3.Angle(t.forward, r * Vector3.forward);
                if (r_diff > rotDiff)
                    t.rotation = r;
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void VirtualizeUnits(ref VirtualArmy.Component army, Transform trans, List<EntityId> followers)
        {
            UnityEngine.Profiling.Profiler.BeginSample("VirtualizeUnits");
            army.IsActive = true;
            var units = army.SimpleUnits;
            units.Clear();

            var inverse = Quaternion.Inverse(trans.rotation);
            var pos = trans.position;
            foreach (var id in followers) {
                Transform t = null;
                if (TryGetComponentObject(id, out t) == false)
                    continue;

                Rigidbody r = null;
                if (TryGetComponentObject(id, out r))
                    r.Sleep();

                var simple = new SimpleUnit();

                simple.RelativePos = (inverse * (t.position - pos)).ToFixedPointVector3();
                simple.RelativeRot = (t.rotation * inverse).ToCompressedQuaternion();

                units.Add(id, simple);

                SendSleepOrder(id, SleepOrderType.Sleep);
            }

            army.SimpleUnits = units;
            var inter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.AlarmInter);
            UpdateLastChecked(ref inter);
            army.AlarmInter = inter;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void RealizeUnits(ref VirtualArmy.Component army, Transform trans)
        {
            UnityEngine.Profiling.Profiler.BeginSample("RealizeUnits");
            army.IsActive = false;
            SyncTroop(army.SimpleUnits, trans);

            foreach (var kvp in army.SimpleUnits) {
                Rigidbody r = null;
                if (TryGetComponentObject(kvp.Key, out r))
                    r.WakeUp();

                SendSleepOrder(kvp.Key, SleepOrderType.WakeUp);
            }

            army.SimpleUnits.Clear();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void AlarmUnits(ref VirtualArmy.Component army, List<EntityId> followers)
        {
            var inter = army.AlarmInter;
            if (CheckTime(ref inter) == false)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("AlarmUnits");
            army.AlarmInter = inter;
            foreach (var id in followers)
                SendSleepOrder(id, SleepOrderType.WakeUp);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        Vector3 GetGrounded(Vector3 pos, float buffer)
        {
            return PhysicsUtils.GetGroundPosition(new Vector3(pos.x, pos.y + 10.0f, pos.z)) + Vector3.up * buffer;
        }

        #region Turrets
        private void UpdateTurrets()
        {
            if (CheckTime(ref turretQuerySet.inter) == false)
                return;

            UpdatePlayerPosition();
            Entities.With(turretQuerySet.group).ForEach(turretAction);
        }

        private void TurretQuery(Entity entity,
                                ref VirtualArmy.Component army,
                                ref TurretHub.Component turret,
                                ref BaseUnitStatus.Component status)
        {
            if (status.State != UnitState.Alive)
                return;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            var pos = trans.position;

            var unit = getNearestPlayer(pos, HexDictionary.HexEdgeLength, selfId:null);
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
        }

        private void VirtualizeTurrests(ref VirtualArmy.Component army, Dictionary<EntityId,TurretInfo> turretsDatas)
        {
            army.IsActive = true;
            SendSleepOrdersToTurret(turretsDatas, SleepOrderType.Sleep);
        }

        private void RealizeTurrets(ref VirtualArmy.Component army, Dictionary<EntityId,TurretInfo> turretsDatas)
        {
            army.IsActive = false;
            var inter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.AlarmInter);
            UpdateLastChecked(ref inter);
            army.AlarmInter = inter;
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
                SendSleepOrder(kvp.Key, SleepOrderType.WakeUp);
        }
        #endregion

        private void SendSleepOrder(EntityId id, SleepOrderType order)
        {
            this.UpdateSystem.SendEvent(new SleepComponent.SleepOrdered.Event(new SleepOrderInfo(order)), id);
        }
    }
}
