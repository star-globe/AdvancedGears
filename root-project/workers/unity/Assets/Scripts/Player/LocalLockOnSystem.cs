using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class LocalLockOnSystem : BaseSearchSystem
    {
        private EntityQuerySet querySet;
        const int frequency = 5; 

        readonly Dictionary<EntityId,BattleCameraInfo> cameraDic = new Dictionary<EntityId,BattleCameraInfo>();
        readonly Dictionary<EntityId,List<EntityId>> lockOnListDic = new Dictionary<EntityId,List<EntityId>>();
        readonly Collider[] colls = new Collider[256];

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<BattleCameraInfo>()), frequency);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            foreach (var kvp in lockOnListDic)
                kvp.Value.Clear();

            Entities.With(querySet.group).ForEach((Entity entity,
                                            ref BaseUnitStatus.Component status,
                                            ref BattleCameraInfo cam) =>
            {
                if (status.State == UnitState.Dead)
                    return;

                var units = getUnitsFromCapsel(status.Side, cam.pos, cam.EndPoint, cam.CapsuleRadius, isEnemy:true, allowDead:false, null, null);
                foreach (var u in units)
                {
                    if (cam.InSide(u.pos))
                        lockOnListDic[cam.entityId].Add(u.id);
                }
            });
        }

        public void RegisterCamera(EntityId entityId, BattleCameraInfo camInfo)
        {
            cameraDic.Add(entityId, camInfo);

            if (lockOnListDic.ContainsKey(entityId) == false)
                lockOnListDic[entityId] = new List<EntityId>();
        }

        public void RemoveCamera(EntityId entityId)
        {
            cameraDic.Remove(entityId);
        }

        public List<EntityId> GetLockOnList(EntityId entityId)
        {
            lockOnListDic.TryGetValue(entityId, out var list);
            return list;
        }
    }

    public struct BattleCameraInfo : IComponentData
    {
        public Vector3 pos;
        public Vector3 forward;
        public float range;
        public float rad;
        public EntityId entityId;

        public bool InSide(in Vector3 pos)
        {
            var diff = pos - pos;
            if (diff.sqrMagnitude > range * range)
                return false;

            if (Vector3.Dot(diff, forward) < 0)
                return false;

            var cross = Vector3.Cross(diff.normalized, forward);
            var sin = Mathf.Sin(rad * Mathf.Deg2Rad);
            return cross.sqrMagnitude < sin * sin;
        }

        public float CapsuleRadius
        {
            get
            {
                return range * Mathf.Tan(rad * Mathf.Deg2Rad);
            }
        }

        public Vector3 EndPoint
        {
            get
            {
                return pos + forward * range;
            }
        }
    }
}
