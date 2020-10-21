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
    internal class AimAnimationLocalSystem : SpatialComponentSystem
    {
        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(ComponentType.ReadOnly<PostureAnimation.Component>(),
                                   ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                   ComponentType.ReadOnly<CombinedAimTracer>());
        }

        protected override void OnUpdate()
        {
            var time = Time.deltaTime;
            Entities.With(group).ForEach((Entity entity,
                                          ref PostureAnimation.Component anim,
                                          ref BaseUnitStatus.Component status) =>
            {
                if (UnitUtils.IsBuilding(status.Type))
                    return;

                var tracer = EntityManager.GetComponentObject<CombinedAimTracer>(entity);
                if (tracer == null)
                    return;

                Vector3 pos = Vector3.zero;
                switch(tracer.AnimTarget.Type)
                {
                    case AnimTargetType.None:
                        return;

                    case AnimTargetType.Position:
                        pos = anim.AnimTarget.Position.ToUnityVector();
                        break;
                }

                tracer.SetTarget(pos);
                tracer.Rotate(time);
            });
        }
    }
}
