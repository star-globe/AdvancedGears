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
    internal class LocalLockOnSystem : SpatialComponentSystem
    {
        private EntityQuerySet querySet;

        private readonly Dictionary<EntityId,BattleCameraInfo> cameraDic = new Dictionary<EntityId,BattleCameraInfo>();
        private readonly Dictionary<EntityId,List<EntityId>> lockOnListDic = new Dictionary<EntityId,List<EntityId>>();

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                            ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                            ComponentType.ReadOnly<Transform>(),
                                            ComponentType.ReadOnly<SpatialEntityId>()), 4);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            foreach (var kvp in lockOnListDic)
                kvp.Value.Clear();

            Entities.With(querySet.group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State == UnitState.Dead)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                foreach (var kvp in cameraDic)
                {
                    if (kvp.Value.InSide(pos))
                        lockOnListDic[kvp.Key].Add(entityId.EntityId);
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

    public struct BattleCameraInfo
    {
        public Transform trans;
        public float range;
        public float rad;

        public bool InSide(in Vector3 pos)
        {
            var diff = pos - trans.position;
            if (diff.sqrMagnitude > range * range)
                return false;

            if (Vector3.Dot(diff, trans.forward) < 0)
                return false;

            var cross = Vector3.Cross(diff.normalized, trans.forward);
            var sin = Mathf.Sin(rad * Mathf.Deg2Rad);
            return cross.sqrMagnitude < sin * sin;
        }
    }
}
