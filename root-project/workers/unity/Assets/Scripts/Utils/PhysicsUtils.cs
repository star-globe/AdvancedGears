using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedGears
{
    public static class PhysicsUtils
    {
        static readonly int mask = LayerMask.GetMask("Ground");
        public static Vector3 GetGroundPosition(Vector3 origin)
        {
            var ray = new Ray(origin, Vector3.down);
            Physics.Raycast(ray, out var hit, mask);

            return hit.point;
        }

        const float buffer = 100.0f;
        public static Vector3 GetGroundPosition(float x, float y, float z)
        {
            return GetGroundPosition(new Vector3( x, y + buffer, z));
        }
    }
}
