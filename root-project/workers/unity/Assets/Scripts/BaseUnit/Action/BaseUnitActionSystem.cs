using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.ReactiveComponents;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using Ex = Extensions;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class BaseUnitActionSystem : ComponentSystem
    {
        private ComponentUpdateSystem updateSystem;
        private EntityQuery group;

        private Vector3 origin;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            updateSystem = World.GetExistingSystem<ComponentUpdateSystem>();

            // ここで基準位置を取る
            origin = World.GetExistingSystem<WorkerSystem>().Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<BaseUnitAction.Component>(),
                ComponentType.ReadOnly<BaseUnitAction.ComponentAuthority>(),
                ComponentType.ReadWrite<BaseUnitPosture.Component>(),
                ComponentType.ReadOnly<BaseUnitPosture.ComponentAuthority>(),
                ComponentType.ReadWrite<GunComponent.Component>(),
                ComponentType.ReadOnly<UnitTransform>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
            group.SetFilter(BaseUnitAction.ComponentAuthority.Authoritative);
            group.SetFilter(BaseUnitPosture.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitAction.Component action,
                                          ref BaseUnitPosture.Component posture,
                                          ref GunComponent.Component gun,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (!action.IsTarget)
                    return;

                var time = Time.time;
                var inter = action.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                action.Interval = inter;

                if (action.EnemyPositions.Count > 0)
                {
                    var epos = action.EnemyPositions[0].ToWorkerPosition(origin);
                    bool updPosture, updGuns;
                    var unit = EntityManager.GetComponentObject<UnitTransform>(entity);
                    Attack(unit, time, action.AngleSpeed, epos, entityId, ref posture, ref gun, out updPosture, out updGuns);

                    //if (updPosture)
                    //    postureData[i] = posture;

                    //if (updGuns)
                    //    gunData[i] = gun;
                }
            });
        }

        void Attack(UnitTransform unit, float time, float angleSpeed, in Vector3 epos, in SpatialEntityId entityId, ref BaseUnitPosture.Component posture, ref GunComponent.Component gun, out bool updPosture, out bool updGuns)
        {
            var pos = posture.Posture;
            var gunsDic = gun.GunsDic;
            updPosture = false;
            updGuns = false;
            foreach (var point in unit.GetKeys())
            {
                GunInfo gunInfo;
                if (gunsDic.TryGetValue(point, out gunInfo) == false)
                    continue;
                PostureData pdata;
                var result = GetSetPosture(unit, point, epos, gunInfo, angleSpeed, out pdata);
                switch (result)
                {
                    case Result.InRange:
                        if (gunInfo.StockBullets == 0)
                            break;
                        var inter = gunInfo.Interval;
                        if (inter.CheckTime(time) == false)
                            break;
                        gunInfo.Interval = inter;
                        var atk = new AttackTargetInfo
                        {
                            Type = 1,
                            TargetPosition = epos.ToImprobableVector3(),
                            Attached = point,
                        };
                        updGuns |= true;
                        updateSystem.SendEvent(new GunComponent.FireTriggered.Event(atk), entityId.EntityId);
                        break;
                    case Result.Rotate:
                        pos.SetData(pdata);
                        updPosture |= true;
                        updateSystem.SendEvent(new BaseUnitPosture.PostureChanged.Event(pdata), entityId.EntityId);
                        break;
                }
            }

            if (updPosture)
                posture.Posture = pos;

            if (updGuns)
                gun.GunsDic = gunsDic;
        }

        enum Result
        {
            OutOfRange = 0,
            InRange,
            Rotate,
        }

        Result GetSetPosture(UnitTransform unit, PosturePoint point, in Vector3 epos, in GunInfo gun, float angleSpeed,
                     out PostureData pdata)
        {
            pdata = new PostureData();
            var postrans = unit.GetPosture(point);
            var cannon = unit.GetCannonTransform(point);
            var result = CheckRange(postrans, cannon, epos, gun.AttackRange, gun.AttackAngle, angleSpeed);
            if (result == Result.Rotate)
            {
                var rot = unit.transform.rotation;
                var list = new List<Ex.Quaternion>(postrans.GetQuaternions().Select(q => q.ToImprobableQuaternion()));
                pdata = new PostureData(point, list);
            }

            return result;
        }

        Result CheckRange(PostureTransform posture, CannonTransform cannon, in Vector3 epos, float range, float angle, float angleSpeed)
        {
            var trans = cannon.Muzzle;
            var diff = epos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return Result.OutOfRange;

            var foward = diff.normalized;
            if (RotateLogic.CheckRotate(cannon.Forward, cannon.HingeAxis, foward, angle))
                return Result.InRange;
            
            posture.Resolve(epos, cannon.Muzzle, angleSpeed * Time.deltaTime);
            return Result.Rotate;
        }
    }
}
