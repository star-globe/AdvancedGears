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
 
        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadWrite<DominationStamina.Component>(),
                ComponentType.ReadOnly<DominationStamina.ComponentAuthority>(),
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
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Dead &&
                    status.Side != UnitSide.None)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                var time = Time.time;
                var inter = domination.Interval;
                if (inter.CheckTime() == false)
                    return;

                domination.Interval = inter;

                float range = domination.Range;
                var staminas = domination.SideStaminas;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var list = getAllUnits(pos, range, UnitType.Commander, UnitType.Advanced);

                var sumsDic = new Dictionary<UnitSide,float>();
                foreach (var unit in list)
                {
                    DominationDevice.Component? comp = null;
                    if (TryGetComponent(unit.id, out comp) == false)
                        continue;
                    
                    switch(comp.Value.Type)
                    {
                        case DominationDeviceType.Capturing:
                            AffectCapture(unit.side, comp.Value.Speed, sumsDic);
                            break;

                        case DominationDeviceType.Jamming:
                            AffectJamming(unit.side, comp.Value.Speed, sumsDic);
                            break;
                    }
                }

                // check over
                var orderedList = sumsDic.OrderByDescending(kvp => kvp.Value).ToList();
                if (orderedList.Count == 0)
                    return;
                    
                var first = orderedList[0];
                var underSum = orderedList.Skip(1).Sum(kvp => kvp.Value);

                if (first.Value <= underSum)
                    return;
                
                var over = first.Value - underSum;
                if (staminas.ContainsKey(first.Key) == false)
                    staminas[first.Key] = over;
                else
                    staminas[first.Key] += over;

                foreach (var k in staminas.Keys) {
                    if (k != first.Key) {
                        var val = staminas[k];
                        staminas[k] = Mathf.Max(0.0f, val - over);
                    }
                }

                // capture
                if (staminas[first.Key] >= domination.MaxStamina) {
                    Capture(entityId.EntityId, first.Key);
                    staminas.Clear();
                }

                domination.SideStaminas = staminas;
            });
        }

        private void AffectCapture(UnitSide side, float speed, Dictionary<UnitSide,float> sumsDic)
        {
            if (sumsDic.ContainsKey(side) == false)
                sumsDic.Add(sisumsDic);

            sumsDic[side] += speed;
        }

        private void AffectJamming(UnitSide side, float speed, Dictionary<UnitSide,float> sumsDic)
        {
            var keys = sumsDic.Keys;
            foreach(var k in keys)
            {
                if (k == side)
                    continue;

                sumsDic[k] -= speed;
            }
        }

        private void Capture(EntityId id, UnitSide side)
        {
            this.CommandSystem.SendCommand(new BaseUnitStatus.ForceState.Request(
                id,
                new ForceStateChange(side, UnitState.Alive))
            );
            this.CommandSystem.SendCommand(new StrongholdSight.SetStrategyVector.Request(
                id,
                new StrategyVector(side, FixedPointVector3.FromUnityVector(Vector3.zero)))
            );
        }
    }
}
