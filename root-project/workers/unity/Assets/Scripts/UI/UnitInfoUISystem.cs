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
    internal class UnitUIInfoSystem : BaseSearchSystem
    {
        private EntityQuery group;

        UnitUICreator unitUICreator = null;
        public UnitUICreator UnitUICreator
        {
            private get { return unitUICreator; }
            set
            {
                if (value == null || unitUICreator != null)
                    return;

                unitUICreator = value;
            }
        }

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

        protected override void OnUpdate()
        {
            if (this.UnitUICreator == null)
                return;

            this.UnitUICreator.ResetAll();

            Entities.With(group).ForEach((Entity entity,
                                          ref BaseUnitStatus.Component status,
                                          ref BaseUnitHealth.Component health,
                                          ref SpatialEntityId entityId) =>
            {
                var trans = EntityManager.GetComponentObject<Transform>(entity);
                if (trans == null)
                    return;

                var range = RangeDictionary.UIRange;
                var diff = trans.position - CharEnumerator.main.transform.position;
                if (diff.sqrtMagnitude > range * range)
                    return;

                var ui = this.UnitUICreator.GetOrCreateHeadUI(entityId.EntityId);
                if (ui == null)
                    return;

                var pos = RectTransformUtility.WorldToScreenPoint(Camera.main, trans.position + ui.Offset);
                ui.SetInfo(pos, health.Health, health.MaxHealth);
            });

            this.UnitUICreator.SleepAllUnused();
        }
    }
}
