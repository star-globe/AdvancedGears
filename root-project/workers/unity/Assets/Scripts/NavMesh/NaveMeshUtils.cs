using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
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

        public static NavPathData CreateData()
        {
            return new NavPathData() { target = Vector3.zero, count = 0, current = 0,};
        }

        public bool IsSetData
        {
            get
            {
                return target != Vector3.zero || count != 0 || current != 0;
            }
        }

        public Vector3 GetCurrentCorner(Vector3[] corners)
        {
            if (current < 0 || count <= 0 || corners == null || current >= corners.Length)
                return Vector3.zero;

            if (current >= count)
                current = count - 1;

            return corners[current];
        }

        public void Next()
        {
            if (current < count - 1)
                current++;
        }
    }
}
