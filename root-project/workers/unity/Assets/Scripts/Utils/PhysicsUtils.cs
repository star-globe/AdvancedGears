using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class PhysicsUtils
    {
        public static readonly int GroundMask = LayerMask.GetMask("Ground");
        public static Vector3 GetGroundPosition(Vector3 origin)
        {
            var ray = new Ray(origin, Vector3.down);
            Physics.Raycast(ray, out var hit, GroundMask);

            return hit.point;
        }

        const float buffer = 100.0f;
        public static Vector3 GetGroundPosition(float x, float y, float z)
        {
            return GetGroundPosition(new Vector3( x, y + buffer, z));
        }

        public static Vector3 GetCrossPointFromVerticalPlane(Vector3 origin, Vector3 target, Vector3[] vertexes)
        {
            if (vertexes == null || vertexes.Length != 2)
                return target;

            var plane = new Plane(vertexes[0], vertexes[1], vertexes[0] + Vector3.up);

            var diff = target - origin;
            if (plane.Raycast(new Ray(origin, target - origin), out float enter) == false)
                return target;

            if (enter > diff.magnitude)
                return target;

            return origin + diff.normalized * enter;
        }
    }
}
