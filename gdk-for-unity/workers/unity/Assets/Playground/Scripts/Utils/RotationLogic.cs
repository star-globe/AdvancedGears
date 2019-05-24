using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playground
{
    public static class RotateLogic
    {
        public static void Rotate(Transform trans, Vector3 foward, float angle = float.MaxValue)
        {
            Rotate(trans, trans.forward, trans.up, foward, angle, false);
        }

        internal static Vector3 Verticalize(Vector3 upAxis, Vector3 src)
        {
            var dot = Vector3.Dot(axisUp, src);
            src -= dot * axisUp;
            return src.normalized;
        }

        public static void Rotate(Transform trans, Vector3 front, Vector3 upAxis, Vector3 tgtFoward, float angle = float.MaxValue, bool fit = true)
        {
            var tgt = Verticalize(upAxis, tgtFoward);
            var frt = Verticalize(upAxis, front);

            var deg = angle != float.MaxValue ? angle * Mathf.Rad2Deg : float.MaxValue;
            var axis = Vector3.Cross(frt, tgt);
            var ang = Vector3.Angle(frt, tgt);
            if (ang < deg)
                deg = ang;

            var u = upAxis;
            if (Vector3.Dot(axis, upAxis) < 0)
                u = -upAxis;

            trans.Rotate(u, deg);

            //if (fit)
            //    trans.rotation = Quaternion.LookRotation(trans.forward, up);
        }

        public static bool CheckRotate(Transform trans, Vector3 up, Vector3 foward, float angle)
        {
            foward = Verticalize(up,foward);

            var d = Vector3.Dot(foward, trans.forward);
            return d > Mathf.Cos(angle);
        }
    }
}

