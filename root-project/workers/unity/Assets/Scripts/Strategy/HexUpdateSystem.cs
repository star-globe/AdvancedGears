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
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

namespace AdvancedGears
{
    [DisableAutoCreation]
    [UpdateInGroup(typeof(FixedUpdateSystemGroup))]
    public class HexUpdateSystem : SpatialComponentSystem
    {
        EntityQuery group;
        IntervalChecker inter;
        const int frequency = 15; 

        protected override void OnCreate()
        {
            base.OnCreate();

            inter = IntervalCheckerInitializer.InitializedChecker(1.0f / frequency);
        }

        protected override void OnUpdate()
        {
            if (inter.CheckTime() == false)
                return;
        }
    }
}
