using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Standardtypes;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class UnitArmyObserveSystem : BaseSearchSystem
    {
        EntityQuery group;

        private Vector3 origin;

        readonly private HashSet<long> requestedIds = new HashSet<long>();

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            // ここで基準位置を取る
            origin = this.Origin;

            group = GetEntityQuery(
                ComponentType.ReadWrite<UnitArmyObserver.Component>(),
                ComponentType.ReadOnly<UnitFactory.Component>(),
                ComponentType.ReadOnly<BaseUnitStatus.Component>(),
                ComponentType.ReadOnly<Transform>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );
        }

        protected override void OnUpdate()
        {
            HandleRequest();
            HandleResponse();
        }

        private void HandleRequest()
        {
            Entities.With(group).ForEach((Unity.Entities.Entity entity,
                                          ref UnitArmyObserver.Component observer,
                                          ref BaseUnitStatus.Component status,
                                          ref SpatialEntityId entityId) =>
            {
                if (status.State != UnitState.Alive)
                    return;

                if (status.Order == OrderType.Idle)
                    return;

                var time = Time.time;
                var inter = observer.Interval;
                if (inter.CheckTime(time) == false)
                    return;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var hq = getNearestAlly(status.Side, pos, RangeDictionary.Get(FixedRangeType.RadioRange), UnitType.HeadQuarter);
                if (hq == null)
                    return;

                Entity hqEntity;
                if (TryGetEntity(hq.id, out hqEntity) == false)
                    return;

                var list = getAllyUnits(status.Side, pos, RangeDictionary.Get(FixedRangeType.BaseRange), UnitType.Commander);
                foreach (var unit in list) {

                    CommanderStatus.Component? comp;
                    if (TryGetComponent(unit.id, out comp) == false)
                        return;

                    var id = entityId.EntityId.Id;
                    if (comp.Value.SuperiorInfo.EntityId.IsValid()) {
                        requestedIds.Remove(id);
                        return;
                    }

                    if (requestedIds.Contains(id))
                        return;

                    var request = new HeadQuarters.AddOrder.Request(hq.id, new OrganizeOrder()
                    {
                        Customer = unit.id,
                        CustomerRank = comp.Value.Rank,
                        Pos = pos.ToFixedPointVector3(),
                        Side = status.Side
                    });

                    this.CommandSystem.SendCommand(request, hqEntity);

                    requestedIds.Add(id);
                }
            });
        }

        private void HandleResponse()
        {
        }
    }
}
