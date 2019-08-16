using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class DominationSystem : BaseSearchSystem
    {
        EntityQuery group;
 
        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            group = GetEntityQuery(
                ComponentType.ReadWrite<DominationStamina.Component>(),
                ComponentType.ReadOnly<DominationStamina.ComponentAuthority>(),
                ComponentType.ReadOnly<FuelComponent.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

            group.SetFilter(DominationStamina.ComponentAuthority.Authoritative);
        }

        protected override void OnUpdate()
        {
            HandleCaputuring();
        }

        void HandleCaputuring()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref DominationStamina.Component domination,
                                          ref FuelComponent.Component fuel,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Dead)
                    return;

                if (status.Side != UnitSide.None)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var time = Time.time;
                var inter = domination.Interval;
                float diff;
                if (inter.CheckTime(time, out diff) == false)
                    return;

                domination.Interval = inter;

                float range = domination.Range;
                //var f_comp = fuel;
                var staminas = domination.SideStaminas;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var list = getUnits(status.Side, pos, range, null, false, UnitType.Commander, UnitType.Advanced);
                foreach (var unit in list)
                {
                    DominationDevice.Component? comp = null;
                    if (TryGetComponent(unit.id, out comp) == false)
                        continue;
                    
                    switch(comp.Value.Type)
                    {
                        case DominationDeviceType.Capturing:
                            AffectCapture(unit.side, comp.Value.Speed, staminas);
                            break;

                        case DominationDeviceType.Jamming:
                            AffectJamming(unit.side, comp.Value.Speed, staminas);
                            break;
                    }
                }

                // check over
                var max = staminas.Max(kvp => kvp.Value);
                if (max.Value >= domination.MaxStamina) {
                    this.Command.SendCommand(new BaseUnitStatus.ForceState.Request(
                        entityId.EntityId,
                        new ForceStateChange(max.Key, UnitState.Alive))
                    );

                    staminas.Clear();
                }
                // check minus
                else {
                    var keys = staminas.Keys;
                    foreach(var k in keys) {
                        if (staminas[k] < 0.0f)
                            staminas[k] = 0.0f;
                    }
                }
            });
        }

        private void AffectCapture(UnitSide side, float speed, Dictionary<UnitSide,float> staminas)
        {
            if (staminas.ContainsKey(side) == false)
                staminas.Add(side, 0.0f);

            staminas[side] += speed;
        }

        private void AffectJamming(UnitSide side, float speed, Dictionary<UnitSide,float> staminas)
        {
            var keys = staminas.keys;
            foreach(var k in keys)
            {
                if (k == side)
                    continue;

                staminas[k] -= speed;
            }
        }
    }
}
