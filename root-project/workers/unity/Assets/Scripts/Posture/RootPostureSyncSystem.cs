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
    internal class RootPostureSyncSystem : SpatialComponentSystem
    {
        private EntityQuerySet querySet;
        EntityQueryBuilder.F_EDD<BaseUnitStatus.Component, PostureRoot.Component> action;
        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<PostureRoot.Component>(),
                                          ComponentType.ReadOnly<PostureRoot.HasAuthority>(),
                                          ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                          ComponentType.ReadOnly<Rigidbody>()), 5);
            action = Query;
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach(action);
        }

        const float rotDiff = 0.01f;
        const float scaleDiff = 0.01f;
        private void Query(Entity entity,
                            ref BaseUnitStatus.Component status,
                            ref PostureRoot.Component posture)
        {
            if (UnitUtils.IsBuilding(status.Type))
                return;

            var rigid = EntityManager.GetComponentObject<Rigidbody>(entity);

            var trans = rigid.transform;
            var rootTrans = posture.RootTrans;

            bool changed = false;
            changed |= (trans.rotation.eulerAngles - rootTrans.Rotation.ToUnityQuaternion().eulerAngles).sqrMagnitude > rotDiff * rotDiff;
            changed |= (trans.localScale - rootTrans.Scale.ToUnityVector()).sqrMagnitude > scaleDiff * scaleDiff;

            if (changed)
                posture.RootTrans = PostureUtils.ConvertTransform(trans);
        }
    }
}
