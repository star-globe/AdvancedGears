using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    
    public static class AnimationRiggingUtils
    {
        public static Vector3 GetRotAxis(MultiAimConstraint multiAimConstraint)
        {
            if (multiAimConstraint == null)
                return Vector3.zero;

            var xAxis = multiAimConstraint.data.constrainedXAxis;
            var yAxis = multiAimConstraint.data.constrainedYAxis;
            var zAxis = multiAimConstraint.data.constrainedZAxis;

            int count = 0;
            count += xAxis ? 1 : 0;
            count += yAxis ? 1 : 0;
            count += zAxis ? 1 : 0;

            if (count != 1)
                return Vector3.zero;

            if (xAxis)
                return Vector3.right;

            if (yAxis)
                return Vector3.up;

            if (zAxis)
                return Vector3.forward;

            return Vector3.zero;
        }

        public static float GetScaleRate(Vector3 scale)
        {
            return scale.magnitude / Vector3.one.magnitude;
        }

        public static Vector3 GetAimAxis(MultiAimConstraintData data)
        {
            var trans = data.constrainedObject;
            if (trans == null)
                return Vector3.zero;

            var axis = data.aimAxis;
            switch (axis)
            {
                case MultiAimConstraintData.Axis.X:
                    return trans.right;

                case MultiAimConstraintData.Axis.X_NEG:
                    return -trans.right;

                case MultiAimConstraintData.Axis.Y:
                    return trans.up;

                case MultiAimConstraintData.Axis.Y_NEG:
                    return -trans.up;

                case MultiAimConstraintData.Axis.Z:
                    return trans.forward;

                case MultiAimConstraintData.Axis.Z_NEG:
                    return -trans.forward;
            }

            return Vector3.zero;
        }
    }
}
