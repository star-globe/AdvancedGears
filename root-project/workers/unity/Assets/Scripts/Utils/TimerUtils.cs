using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class TimerUtils
    {
        static readonly DateTime startTime = new DateTime(2020,1,1,0,0,0, DateTimeKind.Utc);

        public static double CurrentTime
        {
            get
            {
                var span = DateTime.UtcNow - startTime;
                return span.TotalSeconds;
            }
        }
    }

    public class IntervalCounter
    {
        readonly int max;
        int current = 0;
        public IntervalCounter(int count)
        {
            max = count;
            current = 0;
        }

        public bool Check()
        {
            if (current < max)
            {
                current++;
                return false;
            }
            else
            {
                current = 0;
                return true;
            }
        }
    }
}
