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

    public class NavPathContainer
    {
        const float checkRange = 1.0f;
        readonly Dictionary<long, Vector3[]> pointsDic = new Dictionary<long, Vector3[]>();

        public Vector3 CheckNavPathAndTarget(Vector3 target, Vector3 current, float size, long uid, int walkableNavArea, ref NavPathData path)
        {
            if (pointsDic.ContainsKey(uid) == false)
                pointsDic[uid] = new Vector3[256];

            var points = pointsDic[uid];
            if (path.IsSetData == false || (target - path.target).sqrMagnitude > checkRange * checkRange)
            {
                NavMeshUtils.GetNavPoint(current, target, size, walkableNavArea, out var count, points);
                if (count > 0)
                {
                    path.count = count;
                    path.current = 0;
                    path.target = target;
                    return path.GetCurrentCorner(points);
                }
            }
            else
            {
                if ((path.GetCurrentCorner(points) - current).sqrMagnitude < size * size)
                {
                    path.Next();
                }

                return path.GetCurrentCorner(points);
            }

            return target;
        }
    }

    public static class MovementUtils
    {
        /// <summary>
        /// get forward info
        /// </summary>
        /// <param name="diff"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static float get_forward(Vector3 diff, float range, Vector3 forwardVec)
        {
            float forward = 0.0f;
            var buffer = range * RangeDictionary.MoveBufferRate / 2;
            var dot = Vector3.Dot(diff, forwardVec);

            if (dot > range + buffer)
            {
                forward = Mathf.Min((dot - range - buffer) / buffer, 1.0f);
            }
            else if (dot < range - buffer)
            {
                forward = Mathf.Max((dot - range + buffer) / buffer, -1.0f);
            }

            return forward;
        }

        /// <summary>
        /// get rotate info
        /// </summary>
        /// <param name="transform"></param>
        /// <param name="diff"></param>
        /// <param name="angle_range"></param>
        /// <param name="over_rate"></param>
        /// <param name="is_over"></param>
        /// <returns></returns>
        public static int rotate(Transform transform, Vector3 diff, float angle_range, float over_rate, out bool is_over)
        {
            is_over = false;
            var rot = RotateLogic.GetAngle(transform.up, transform.forward, diff.normalized);
            var sqrtRot = rot * rot;
            var sqrtRange = angle_range * angle_range;
            if (sqrtRot < sqrtRange)
            {
                return 0;
            }

            is_over = sqrtRot > sqrtRange * over_rate * over_rate;
            return rot < 0 ? -1 : 1;
        }
    }
}
