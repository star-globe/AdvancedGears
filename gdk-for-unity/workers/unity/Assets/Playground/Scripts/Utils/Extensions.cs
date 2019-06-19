using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playground
{
    static class Extensions
    {
        public static Quaternion ToUnityQuaternion(this Improbable.Transform.Quaternion quo)
        {
            return new Quaternion(quo.X, quo.Y, quo.Z, quo.W);
        }

        public static Improbable.Transform.Quaternion ToImprobableQuaternion(this Quaternion quo)
        {
            return new Improbable.Transform.Quaternion(quo.w, quo.x, quo.y, quo.z);
        }

        public static Improbable.Vector3f ToImprobableVector3(this Vector3 vec)
        {
            return new Improbable.Vector3f(vec.x, vec.y, vec.z);
        }

        public static float SqrMagnitude (this Improbable.Vector3f vec)
        {
            return (vec.x * vec.x) + (vec.y * vec.y) + (vec.z * vec.z);
        }

        public static bool CheckTime(this ref IntervalChecker inter, float current)
        {
            if (current - inter.LastChecked < inter.Interval)
                return false;

            inter.LastChecked = current + RandomInterval.GetRandom(inter.Interval);
            return true;
        }

        public static void SetData(this ref PostureInfo info, PostureData data)
        {
            if (info.Datas.ContainsKey(data.Point))
                info.Datas[data.Point] = data;
            else
                info.Datas.Add(data.Point, data);
        }

        public static void Resolve(this PostureTransform posture, Vector3 position, Transform effector, float angleSpeed = float.MaxValue)
        {
            if (posture.IsSet == false)
                return;

            var connectors = posture.Connectors;
            int length = connectors.Length;
            if (length == 0)
                return;

            Vector3 dmy = position;
            System.Action<AttachedTransform> rotate = (connector) =>
            {
                RotateAndMoveEffector(connector, position, effector, angleSpeed);
            };

            foreach (var c in connectors.Reverse())
                rotate(c);

            foreach (var c in connectors)
                rotate(c);
        }

        internal static Vector3 RotateAndMoveEffector(AttachedTransform attached, Vector3 tgt, Transform effector, float angleSpeed = float.MaxValue)
        {
            if (attached.HingeAxis == Vector3.zero)
                return Vector3.zero;

            var foward = (tgt - attached.transform.position).normalized;
            var vec = effector.position - attached.transform.position;
            var length = vec.magnitude;
            RotateLogic.Rotate(attached.transform, vec.normalized, attached.HingeAxis, foward, attached.Constrain, angleSpeed);

            return tgt - foward * length;
        }

        public static List<Improbable.Transform.Quaternion> GetAllRotates(this UnitTransform unit, PosturePoint point)
        {
            if (unit.PostureDic.ContainsKey(point) == false)
                return new List<Improbable.Transform.Quaternion>();

            return unit.PostureDic[point].Connectors.Select(c => c.transform.rotation.ToImprobableQuaternion()).ToList();
        }

        public static T[] GetComponentsInChildrenWithoutSelf<T>(this GameObject self) where T : Component
        {
            return self.GetComponentsInChildren<T>().Where(c => self != c.gameObject).ToArray();
        }

        public static T GetComponentInChildrenWithoutSelf<T>(this GameObject self) where T : Component
        {
            return self.GetComponentsInChildrenWithoutSelf<T>().FirstOrDefault();
        }

        public static float GetAttackRange(this GunComponent.Component gun)
        {
            float length = 0;
            foreach (var kvp in gun.GunsDic)
            {
                var l = kvp.Value.AttackRange;
                if (length < l)
                    length = l;
            }

            return length;
        }

        public static bool NeedsFollowers(this FollowerInfo info, int num)
        {
            bool tof = false;
            tof |= info.Followers.Count < num;
            tof |= info.UnderCommanders.Count < num;
            return tof;
        }

        public static int EmptyCapacity(this FuelComponent.Component fuel)
        {
            return fuel.MaxFuel - fuel.Fuel;
        }

        public static float FuelRate(this FuelComponent.Component fuel)
        {
            return fuel.Fuel * 1.0f / fuel.MaxFuel;
        }
    }
}

