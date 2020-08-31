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

        readonly private HashSet<long> requestedIds = new HashSet<long>();

        protected override void OnCreate()
        {
            base.OnCreate();

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

                var inter = observer.Interval;
                if (CheckTime(ref inter) == false)
                    return;

                observer.Interval = inter;

                var trans = EntityManager.GetComponentObject<Transform>(entity);
                var pos = trans.position;

                var hq = getNearestAlly(entityId.EntityId, status.Side, pos, RangeDictionary.Get(FixedRangeType.RadioRange), allowDead:false, UnitType.HeadQuarter);
                if (hq == null)
                    return;

                Entity hqEntity;
                if (TryGetEntity(hq.id, out hqEntity) == false)
                    return;

                var list = getAllyUnits(status.Side, pos, RangeDictionary.Get(FixedRangeType.BaseRange), allowDead:false, UnitType.Commander);
                foreach (var unit in list) {
                    CommanderTeam.Component? team;
                    if (TryGetComponent(unit.id, out team) == false)
                        return;

                    var id = entityId.EntityId.Id;
                    if (team.Value.SuperiorInfo.EntityId.IsValid()) {
                        requestedIds.Remove(id);
                        return;
                    }

                    if (requestedIds.Contains(id))
                        return;

                    var request = new HeadQuarters.AddOrder.Request(hq.id, new OrganizeOrder()
                    {
                        Customer = unit.id,
                        CustomerRank = status.Rank,
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
