using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Unity.Collections;

namespace AdvancedGears
{
    public static class NavMeshUtils
    {
        static readonly NavMeshPath path = new NavMeshPath();
        public static Vector3 GetNavPoint(Vector3 current, Vector3 target, float range, int area)
        {
            return GetNavPoint(current, target, range, area, out var count, points:null);
        }

        public static Vector3 GetNavPoint(Vector3 current, Vector3 target, float range, int area, out int count, Vector3[] points)
        {
            Vector3 tgt = target;
            var random = MovementDictionary.RandomCount;
            for (var i = 0; i < random; i++)
            {
                path.ClearCorners();
                if (NavMesh.CalculatePath(current, tgt, area, path))
                {
                    if (points != null)
                        count = path.GetCornersNonAlloc(points);
                    else
                        count = 0;

                    return tgt;
                }

                var circle = UnityEngine.Random.insideUnitCircle;
                tgt += new Vector3(circle.x, 0, circle.y) * range;
            }

            count = 0;
            return current;
        }

        public static int GetNavArea(string areaName)
        {
            return 1 >> NavMesh.GetAreaFromName(areaName);
        }
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
