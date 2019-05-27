using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playground
{
    public static class RotateLogic
    {
        public static void Rotate(Transform trans, Vector3 foward, float max = float.MaxValue)
        {
            Rotate(trans, trans.forward, trans.up, foward, max: max);
        }

        public static void Rotate(Transform trans, Vector3 front, Vector3 upAxis, Vector3 tgtFoward, ConnectorConstrain constrain, float angleSpeed = float.MaxValue)
        {
            if (constrain == null)
                Rotate(trans, front, upAxis, tgtFoward, angleSpeed:angleSpeed);
            else
                Rotate(trans, front, upAxis, tgtFoward, constrain.OrderVector, constrain.Min, constrain.Max, angleSpeed:angleSpeed);
        }

        internal static Vector3 Verticalize(Vector3 upAxis, Vector3 src)
        {
            var dot = Vector3.Dot(upAxis, src);
            src -= dot * upAxis;
            return src.normalized;
        }

        public static float GetAngle(Vector3 upAxis, Vector3 front, Vector3 target)
        {
            bool reversed;
            return GetAngle(upAxis, front, target, out reversed);
        }

        public static float GetAngle(Vector3 upAxis, Vector3 front, Vector3 target, out bool reversed)
        {
            reversed = false;
            var tgt = Verticalize(upAxis, target);
            var frt = Verticalize(upAxis, front);

            var axis = Vector3.Cross(frt, tgt);
            var ang = Vector3.Angle(frt, tgt);
            //
            if (Vector3.Dot(axis, upAxis) < 0)
            {
                reversed = true;
                ang = -ang;
            }

            return ang;
        }

        public static void Rotate(Transform trans, Vector3 front, Vector3 upAxis, Vector3 tgtFoward,
                                    Vector3? orderAxis = null, float min = float.MinValue, float max = float.MaxValue, float angleSpeed = float.MaxValue)
        {
            var ang = GetAngle(upAxis, front, tgtFoward);

            if (orderAxis == null)
                orderAxis = front;

            if (orderAxis != Vector3.zero && Vector3.Dot(orderAxis.Value, upAxis) <= 0.0001f)
            {
                var order = GetAngle(upAxis, front, orderAxis.Value);
                if (min != float.MinValue)
                    min += order;
                if (max != float.MaxValue)
                    max += order;

                ang = Mathf.Clamp(ang, min, max);

                if (angleSpeed != float.MaxValue)
                    ang = Mathf.Clamp(ang, -angleSpeed, angleSpeed);
            }

            trans.Rotate(upAxis, ang, Space.World);
        }

        public static bool CheckRotate(Vector3 front, Vector3 up, Vector3 tgtFoward, float angle)
        {
            tgtFoward = Verticalize(up,tgtFoward);

            var d = Vector3.Dot(tgtFoward, front);
            return d > Mathf.Cos(angle);
        }
    }
}

