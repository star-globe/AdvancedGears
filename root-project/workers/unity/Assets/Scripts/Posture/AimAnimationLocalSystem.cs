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

        float deltaTime = 0;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(ComponentType.ReadOnly<PostureAnimation.Component>(),
                                   ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                   ComponentType.ReadOnly<CombinedAimTracer>());
        }

        protected override void OnUpdate()
        {
            deltaTime = Time.DeltaTime;

            Entities.With(group).ForEach((Entity entity,
                                          ref PostureAnimation.Component anim,
                                          ref BaseUnitStatus.Component status) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                var tracer = EntityManager.GetComponentObject<CombinedAimTracer>(entity);
                if (tracer == null)
                    return;

                Vector3? pos = null;
                switch(anim.AnimTarget.Type)
                {
                    case AnimTargetType.None:
                        break;

                    case AnimTargetType.Position:
                        pos = anim.AnimTarget.Position.ToWorkerPosition(this.Origin);
                        break;
                }

                tracer.SetAimTarget(pos);
                tracer.Rotate(deltaTime);
            });
        }
    }
}
