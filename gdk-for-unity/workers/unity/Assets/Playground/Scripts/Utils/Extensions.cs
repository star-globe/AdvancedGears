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

        public static void Resolve(this PostureTransform posture, Vector3 position)
        {
            if (posture.IsSet == false)
                return;

            var connectors = posture.Connectors;
            int length = connectors.Length;
            if (length == 0)
                return;

            Vector3 end = position;
            Vector3 start = connectors[0].transform.position;

            Vector3 dmy = end;
            for (int j = length - 1; j >= 0; j--)
            {
                dmy = SetAndGetDummyPosition(posture, connectors[j], dmy, true);
            }
            dmy = start;
            for (int i = 0; i < length - 1; i++)
            {
                dmy = SetAndGetDummyPosition(posture, connectors[i], dmy, false);
            }
        }

        internal static Vector3 SetAndGetDummyPosition(this PostureTransform posture, AttachedTransform attached, Vector3 tgt, bool isFoward)
        {
            // foward to back de syoriga tigau

            if (attached.HingeAxis == Vector3.zero)
                return Vector3.zero;

            var tgtFoward = (tgt - attached.transform.position).normalized;
            var vec = attached.TargetVector;
            RotateLogic.Rotate(attached.transform, vec.normalized, attached.HingeAxis, tgtFoward);

            return tgt - tgtFoward * vec.Length;
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
    }
}

