using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.Core.Commands;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Worker.CInterop;
using Improbable.Worker.CInterop.Query;
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
                ).FilterResults(Positioin.ComponentId, PlayerInfo.ComponentId);

            
        }
    }
}