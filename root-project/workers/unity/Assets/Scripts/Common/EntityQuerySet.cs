using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class EntityQuerySet
    {
        public EntityQuery group;
        public IntervalChecker inter;

        public EntityQuerySet(EntityQuery query, int period)
        {
            this.group = query;
            this.inter = IntervalCheckerInitializer.InitializedChecker(period);
        }

        public EntityQuerySet(EntityQuery query, float inter)
        {
            this.group = query;
            this.inter = IntervalCheckerInitializer.InitializedChecker(inter);
        }

    }
}
