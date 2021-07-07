using System;
using System.Collections;
using System.Collections.Generic;
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
        EntityQueryBuilder.F_EDDD<DominationStamina.Component, BaseUnitStatus.Component, SpatialEntityId> deviceAction;

        StrategyHexAccessPortalUpdateSystem portalUpdateSytem = null;
        private Dictionary<uint, HexIndex> HexIndexes => portalUpdateSytem?.HexIndexes;
        private readonly Dictionary<UnitSide,float> sumsDic = new Dictionary<UnitSide,float>();

        private readonly List<UnitSide> keys = new List<UnitSide>();

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
            deviceAction = DeviceQuery;
            portalUpdateSytem = World.GetExistingSystem<StrategyHexAccessPortalUpdateSystem>();
        }

        protected override void OnUpdate()
        {
            HandleCaputure();
        }

        void HandleCaputure()
        {
            if (CheckTime(ref deviceGroup.inter) == false)
                return;

            Entities.With(deviceGroup.group).ForEach(deviceAction);
        }
            
        private void DeviceQuery(Unity.Entities.Entity entity,
                                 ref DominationStamina.Component domination,
                                 ref BaseUnitStatus.Component status,
                                 ref SpatialEntityId entityId)
        {
            if (status.Side != UnitSide.None)
                return;

            if (status.Type != UnitType.Stronghold)
                return;

            float range = domination.Range;
            var staminas = domination.SideStaminas;

            var trans = EntityManager.GetComponentObject<Transform>(entity);
            var pos = trans.position;
            var list = getAllUnits(pos, range, allowDead:false, AttackLogicDictionary.DominationUnitTypes);

            sumsDic.Clear();
            foreach (var unit in list)
            {
                DominationDevice.Component? comp = null;
                if (TryGetComponent(unit.id, out comp) == false)
                    continue;

                var speed = 1.5f;

                switch(comp.Value.Type)
                {
                    case DominationDeviceType.Capturing:
                        AffectCapture(unit.side, speed, sumsDic);
                        break;

                    case DominationDeviceType.Jamming:
                        AffectJamming(unit.side, speed, sumsDic);
                        break;
                }
            }

            if (this.HexIndexes != null)
            {
                foreach (var kvp in this.HexIndexes)
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
            if (sumsDic.Count == 0)
                return;

            UnitSide firstSide = UnitSide.None;
            float firstValue = 0.0f;
            float underSum = 0.0f;

            foreach(var kvp in sumsDic) {
                if (kvp.Value > firstValue) {
                    underSum += firstValue;
                    firstSide = kvp.Key;
                    firstValue = kvp.Value;
                }
                else {
                    underSum += kvp.Value;
                }
            }

            if (firstValue <= underSum)
                return;
                
            var over = firstValue - underSum;
            if (staminas.ContainsKey(firstSide) == false)
                staminas[firstSide] = over;
            else
                staminas[firstSide] += over;

            this.keys.Clear();
            this.keys.AddRange(staminas.Keys);

            foreach (var k in this.keys) {
                if (k != firstSide && staminas.ContainsKey(k)) {
                    var val = staminas[k];
                    staminas[k] = Mathf.Max(0.0f, val - over);
                }
            }

            // capture
            if (staminas[firstSide] >= domination.MaxStamina) {
                Capture(entityId.EntityId, firstSide);
                staminas.Clear();
            }

            domination.SideStaminas = staminas;
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
        }
    }
}
