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

        readonly Dictionary<EntityId,List<EntityId>> lockOnListDic = new Dictionary<EntityId,List<EntityId>>();
        readonly Collider[] colls = new Collider[256];

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<Transform>(),
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

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                if (trans == null)
                    return;

                var units = getUnitsFromCapsel(status.Side, trans.position, cam.GetEndPoint(trans), cam.CapsuleRadius, isEnemy:true, allowDead:false, null, null);
                foreach (var u in units)
                {
                    if (cam.InSide(u.pos, trans.position, trans.forward))
                        lockOnListDic[cam.entityId].Add(u.id);
                }
            });
        }

        public List<EntityId> GetLockOnList(EntityId entityId)
        {
            lockOnListDic.TryGetValue(entityId, out var list);
            return list;
        }
    }

    [Serializable]
    public struct BattleCameraInfo : IComponentData
    {
        public float range;
        public float rad;
        public EntityId entityId;

        public bool InSide(in Vector3 pos, in Vector3 from, in Vector3 forward)
        {
            var diff = pos - from;
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

        public Vector3 GetEndPoint(Transform trans)
        {
            if (trans == null) {
                Debug.Log("GetEndPoint:Transform is null.");
                return Vector3.zero;
            }

            return trans.position + trans.forward * range;
        }
    }
}
