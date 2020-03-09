using UnityEngine;
using System.Collections;

namespace AdvancedGears
{
    public static class FixedParams
    {
        public static readonly float PlayerInterestLimit = 250.0f;
        public static readonly float WorldInterestLimit = PlayerInterestLimit * 10.0f;
        public static readonly float WorldInterestFrequency = 1.0f;
        public static readonly float AbyssHeight = -1000.0f;
    }
}
