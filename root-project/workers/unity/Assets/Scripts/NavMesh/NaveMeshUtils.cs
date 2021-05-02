using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    public static class NaveMeshUtils
    {
        
    }

    public struct NavPathData : IComponentData
    {
        public Vector3 target;
        public int count;
        public int current;
        public NativeArray<Vector3> corners;

        public Vector3 CurrentCorner
        {
            get
            {
                if (current < 0 || count <= 0 || corners == null || current >= corners.Length)
                    return Vector3.zero;

                if (current >= count)
                    current = count - 1;

                return corners[current];
            }
        }

        public void Next()
        {
            if (current < count - 1)
                current++;
        }
    }
}
