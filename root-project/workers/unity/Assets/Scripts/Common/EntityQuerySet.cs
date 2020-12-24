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
        public double deltaTime;

        public EntityQuerySet(EntityQuery query, int period, double current = double.MinValue)
        {
            this.group = query;
            this.inter = IntervalCheckerInitializer.InitializedChecker(period);
            this.deltaTime = current;
        }

        public EntityQuerySet(EntityQuery query, float inter, double current = double.MinValue)
        {
            this.group = query;
            this.inter = IntervalCheckerInitializer.InitializedChecker(inter);
            this.deltaTime = current;
        }

        public double GetDelta(double current)
        {
            double delta = 0;
            if (this.deltaTime != double.MinValue)
                delta = this.deltaTime - current;

            this.deltaTime = current;
            return delta;
        }
    }
}
