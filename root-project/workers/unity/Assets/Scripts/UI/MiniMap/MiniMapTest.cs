using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.QueryBasedInterest;
using ImprobableEntityQuery = Improbable.Worker.CInterop.Query.EntityQuery;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class MinimapTest
    {
        void Test()
        {
            var playerQuery = InterestQuery.Query(
                Constraint.All(
                    Constraint.Component<PlayerInfo.Component>(),
                    Constraint.RelativeSphere(20))
                ).FilterResults(Position.ComponentId, PlayerInfo.ComponentId);

            var minimapQuery = InterestQuery.Query(
                Constraint.All(
                    Constraint.Component<MinimapRepresentaion.Component>(),
                    Constraint.RelativeBox(50, double.PositiveInfinity, 50))
                ).FilterResults(Position.ComponentId, MinimapRepresentaion.ComponentId);

            var interestTemplate = InterestTemplate.Create()
                .AddQueries<PlayerControls.Component>(playerQuery, minimapQuery);

            var playerTemplate = new EntityTemplate();
            playerTemplate.AddComponent(interestTemplate.ToSnapshot(), WorkerUtils.UnityGameLogic);
        }
    }

    class MinimapSystem : SpatialComponentSystem
    {
        EntityQuery group;

        protected override void OnCreate()
        {
            base.OnCreate();

            group = GetEntityQuery(
                ComponentType.ReadOnly<Interest.Component>(),
                ComponentType.ReadOnly<PlayerControls.Component>(),
                ComponentType.ReadOnly<SpatialEntityId>()
            );

        }

        protected override void OnUpdate()
        {
            Entities.With(group).ForEach((Entity entity,
                                          ref Interest.Component interest,
                                          ref PlayerControls.Component controls,
                                          ref SpatialEntityId entityId) =>
            {
                var dic = interest.ComponentInterest;
                foreach (var kvp in dic)
                {
                    var uid = kvp.Key;
                    var value = kvp.Value;

                    foreach(var q in value.Queries)
                    {
                        var list = q.ResultComponentId;
                    }
                }
            });
        }
    }
}
