using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using Unity.Collections;
using Unity.Entities;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class HexActiveManageSystem : HexUpdateBaseSystem
    {
        EntityQuerySet playerQuerySet;
        const int frequency = 5;

        protected override void OnCreate()
        {
            base.OnCreate();

            playerQuerySet = new EntityQuerySet(GetEntityQuery(
                                                ComponentType.ReadOnly<PlayerInfo.Component>(),
                                                ComponentType.ReadOnly<Position.Component>(),
                                                ComponentType.ReadOnly<SpatialEntityId>())
                                                , frequency);
        }

        protected override void OnUpdate()
        {
             PlayerCheck();
        }

        private void PlayerCheck()
        {
            if (base.HexDic == null)
                return;

            if (CheckTime(ref playerQuerySet.inter) == false)
                return;

            HashSet<uint> activeIndexes = new HashSet<uint>();
            Entities.With(playerQuerySet.group).ForEach((Entity entity,
                                      ref PlayerInfo.Component player,
                                      ref Position.Component position,
                                      ref SpatialEntityId entityId) =>
            {
                var pos = position.Coords.ToUnityVector() + this.Origin;
                foreach (var kvp in this.HexDic) {
                    if (HexUtils.IsInsideHex(this.Origin, kvp.Key, pos))
                        activeIndexes.Add(kvp.Key);
                }
            });

            foreach (var kvp in this.HexDic)
            {
                bool isActivate = activeIndexes.Contains(kvp.Key);
                if (kvp.Value.isActive != isActivate)
                    this.UpdateSystem.SendEvent(new HexPower.HexActiveChange.Event(new HexActiveChange() { IsActive = isActivate}), kvp.Value.EntityId.EntityId);
            }
        }
    }
}
