using System;
using System.Collections.Generic;
using System.Linq;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class TransformUtils
    {
        private static FixedPointVector3 one = FixedPointVector3.Zero;
        public static FixedPointVector3 OneVector
        {
            get
            {
                if (one == FixedPointVector3.Zero)
                    one = Vector3.one.ToFixedPointVector3();

                return one;
            }
        }

        public static CompressedQuaternion ToAngleAxis(float angle, Vector3 axis)
        {
            return Quaternion.AngleAxis(angle, axis).ToCompressedQuaternion();
        }
    }
}
