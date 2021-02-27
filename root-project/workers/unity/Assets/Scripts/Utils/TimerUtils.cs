using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class TimerUtils
    {
        
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
