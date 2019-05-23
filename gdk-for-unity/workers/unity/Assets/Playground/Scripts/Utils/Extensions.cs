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

            if (length == 1)
            {
                SetAndGetDummyPosition(posture, connectors[0], null, position);
                return;
            }

            Vector3 dmy = SetAndGetDummyPosition(posture, connectors[length - 2], connectors[length - 1], position);
            for (int j = length - 3; j > 0; j--)
            {
                if (dmy == Vector3.zero)
                    break;

                dmy = SetAndGetDummyPosition(posture, connectors[j], connectors[j + 1], dmy);
            }
            for (int i = 0; i < length - 2; i++)
            {
                if (dmy == Vector3.zero)
                    break;

                dmy = SetAndGetDummyPosition(posture, connectors[i + 1], connectors[i], dmy);
            }
        }

        internal static Vector3 SetAndGetDummyPosition(this PostureTransform posture, AttachedTransform attached, AttachedTransform next, Vector3 tgt)
        {
            if (next == null || attached.HingeAxis == Vector3.zero)
                return Vector3.zero;

            var foward = (tgt - attached.transform.position).normalized;
            RotateLogic.Rotate(attached.transform, attached.transform.foward, attached.HingeAxis, foward);

            return attached.transform.position + (tgt - next.transform.position);
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

