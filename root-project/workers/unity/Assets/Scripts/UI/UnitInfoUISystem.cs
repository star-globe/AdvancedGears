using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
using AdvancedGears;

namespace AdvancedGears.UI
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    internal class UnitUIInfoSystem : BaseUISystem<UnitHeadUI>
    {
        protected override UIType uiType => UIType.HeadStatus;

        private EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<BaseUnitHealth.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        const float size = 1.1f;
        const float depth = 1000.0f;
        Bounds viewBounds = new Bounds(new Vector3(0.5f,0.5f, depth/2), new Vector3(size,size, depth));

        protected override void OnUpdate()
        {
            base.OnUpdate();
        }

        protected override void UpdateAction()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitHealth.Component health,
                                          ref SpatialEntityId entityId) =>
            {
                var trans = EntityManager.GetComponentObject<Transform>(entity);
                if (trans == null)
                    return;

                var range = RangeDictionary.UIRange;
                var diff = trans.position - Camera.main.transform.position;
                if (diff.sqrMagnitude > range * range)
                    return;

                var camera = Camera.main;
                var view = camera.WorldToViewportPoint(trans.position);
                if (viewBounds.Contains(view) == false)
                    return;

                var ui = GetOrCreateUI(entityId.EntityId);
                if (ui == null)
                    return;

                var pos = RectTransformUtility.WorldToScreenPoint(camera, trans.position + ui.Offset);
                ui.SetInfo(pos, health.Health, health.MaxHealth);
            });
        }
    }

    abstract class BaseUISystem<T> : BaseSearchSystem where T : Component,IUIObject
    {
        UnitUICreator unitUICreator;
        UnitUICreator UnitUICreator
        {
            get
            {
                if (unitUICreator == null)
                {
                    unitUICreator = UnitUICreator.Instance;
                    if (unitUICreator != null)
                    {
                        if (unitUICreator.ContainsType(this.uiType) == false)
                            unitUICreator.AddContainer<T>(this.uiType);
                        else
                            Debug.LogErrorFormat("UIType:{0} was Registered.", this.uiType);
                    }
                }

                return unitUICreator;
            }
        }

        protected abstract UIType uiType { get; }

        protected virtual Transform parent => null;

        protected override void OnUpdate()
        {
            if (this.UnitUICreator == null)
                return;

            this.UnitUICreator.ResetAll(this.uiType);

            UpdateAction();

            this.UnitUICreator.SleepAllUnused(this.uiType);
        }

        protected abstract void UpdateAction();

        protected T GetOrCreateUI(EntityId entityId)
        {
            return this.UnitUICreator.GetOrCreateUI(this.uiType, entityId, parent) as T;
        }
    }
}
