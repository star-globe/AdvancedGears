using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AdvancedGears
{
    public static class PhysicsUtils
    {
        public static Vector3 GetGroundPosition(Vector3 origin)
        {
            var ray = new Ray(origin, Vector3.down);
            RaycastHit hit;
            Physics.Raycast(ray, out hit, LayerMask.GetMask("Ground"));

            return hit.point;
        }
    }
}
