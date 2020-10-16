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
    internal class PostureAnimationSyncSystem : SpatialComponentSystem
    {
        private EntityQuerySet querySet;

        protected override void OnCreate()
        {
            base.OnCreate();

            querySet = new EntityQuerySet(GetEntityQuery(
                                          ComponentType.ReadWrite<PostureAnimation.Component>(),
                                          ComponentType.ReadOnly<PostureAnimation.HasAuthority>(),
                                          ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                          ComponentType.ReadOnly<PostureBoneContainer>()), 3);
        }

        protected override void OnUpdate()
        {
            if (CheckTime(ref querySet.inter) == false)
                return;

            Entities.With(querySet.group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref PostureAnimation.Component anim) =>
            {
                if (UnitUtils.IsBuilding(status.Type))
                    return;

                var container = EntityManager.GetComponentObject<PostureBoneContainer>(entity);
                if (container == null)
                    return;

                var boneMap = anim.BoneMap;

                bool changed = false;
                foreach (var bone in container.Bones)
                {
                    bool localChanged = false;
                    if (boneMap.ContainsKey(bone.hash) == false)
                    {
                        localChanged = true;
                    }
                    else
                    {
                        var trans = boneMap[bone.hash];

                        localChanged |= bone.transform.position != trans.Position.ToWorkerPosition(this.Origin);
                        localChanged |= bone.transform.rotation != trans.Rotation.ToUnityQuaternion();
                        localChanged |= bone.transform.localScale != trans.Scale.ToUnityVector();
                    }

                    if (localChanged)
                        boneMap[bone.hash] = PostureUtils.ConvertTransform(bone.transform);

                    changed |= localChanged;
                }

                if (changed)
                {
                    anim.BoneMap = boneMap;
                }
            });
        }
    }
}
