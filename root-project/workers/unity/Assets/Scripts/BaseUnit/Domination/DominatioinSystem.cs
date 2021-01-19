using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Worker.CInterop;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class DominationSystem : BaseSearchSystem
    {
        EntityQuerySet deviceGroup;
        EntityQuerySet hexPowerGroup;

        protected override void OnCreate()
        {
            base.OnCreate();

            deviceGroup = new EntityQuerySet(GetEntityQuery(
                                             ComponentType.ReadWrite<DominationStamina.Component>(),
                                             ComponentType.ReadOnly<DominationStamina.HasAuthority>(),
                                             ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                                             ComponentType.ReadOnly<Transform>(),
                                             ComponentType.ReadOnly<SpatialEntityId>()
                                             ), 1.0f);

            hexPowerGroup = new EntityQuerySet(GetEntityQuery(
                                             ComponentType.ReadOnly<StrategyHexAccessPortal.Component>()
                                             ), 1.0f);
        }

        protected override void OnUpdate()
        {
            GatherPortalData();
            HandleCaputure();
        }

        void HandleCaputure()
        {
            if (CheckTime(ref deviceGroup.inter) == false)
                return;

            Entities.With(deviceGroup.group).ForEach((Unity.Entities.Entity entity,
                                          ref DominationStamina.Component domination,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.Side != UnitSide.None)
                    return;

                if (status.Type != UnitType.Stronghold)
                    return;

                float range = domination.Range;
                var staminas = domination.SideStaminas;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;
                var list = getAllUnits(pos, range, allowDead:false, UnitType.Commander, UnitType.Advanced);

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

                if (hexIndexes != null)
                {
                    foreach (var kvp in hexIndexes)
                    {
                        if (HexUtils.IsInsideHex(this.Origin, kvp.Key, pos, HexDictionary.HexEdgeLength) == false)
                            continue;

                        var hex = kvp.Value;
                        if (HexUtils.TryGetOneSidePower(hex, out var side, out var val))
                        {
                            if (sumsDic.ContainsKey(side))
                                sumsDic[side] += val;
                            else
                                sumsDic[side] = val;
                        }
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

                var keys = staminas.Keys.ToArray();
                foreach (var k in keys) {
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

        private Dictionary<uint, HexIndex> hexIndexes;

        void GatherPortalData()
        {
            if (CheckTime(ref hexPowerGroup.inter) == false)
                return;

            Entities.With(hexPowerGroup.group).ForEach((Unity.Entities.Entity entity,
                                          ref StrategyHexAccessPortal.Component portal) =>
            {
                hexIndexes = portal.HexIndexes;
            });
        }

        private void AffectCapture(UnitSide side, float speed, Dictionary<UnitSide,float> sumsDic)
        {
            if (sumsDic.ContainsKey(side) == false)
                sumsDic.Add(side, 0.0f);

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
            this.UpdateSystem.SendEvent(new BaseUnitStatus.ForceState.Event(new ForceStateChange(side, UnitState.Alive)), id);
            this.CommandSystem.SendCommand(new StrongholdSight.SetStrategyVector.Request(
                id,
                new StrategyVector(side, FixedPointVector3.FromUnityVector(Vector3.zero)))
            );
        }
    }
}
