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

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<PostureRoot.Component>(),
                                          ComponentType.ReadOnly<PostureRoot.HasAuthority>(),
                                          ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                          ComponentType.ReadOnly<Rigidbody>()), 5);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref PostureRoot.Component posture) =>
            {
                if (UnitUtils.IsBuilding(status.Type))
                    return;

                var rigid = EntityManager.GetComponentObject<Rigidbody>(entity);

                var trans = rigid.transform;
                var rootTrans = posture.RootTrans;

                bool changed = false;
                changed |= trans.rotation != rootTrans.Rotation.ToUnityQuaternion();
                changed |= trans.localScale != rootTrans.Scale.ToUnityVector();

                if (changed)
                {
                    var local = PostureUtils.ConvertTransform(trans);

                    DebugUtils.RandomlyLog($"angle:{local.Rotation.ToUnityQuaternion().eulerAngles}");

                    posture.RootTrans = PostureUtils.ConvertTransform(trans);
                }
            });
        }
    }
}
