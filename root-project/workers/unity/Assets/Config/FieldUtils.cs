using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    public static class FieldUtils
    {
        public static Vector3 GetGround(int x, int z, float inter, float maxHeight, Vector3 origin)
        {
            return PhysicsUtils.GetGroundPosition(new Vector3(x * inter, maxHeight, z * inter) + origin); 
        }
    }
}
