using System.Collections;
using System.Collections.Generic;
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
    }
}

