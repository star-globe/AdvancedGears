using System.Collections;
using Improbable;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class PostureUtils
    {
        public static CompressedLocalTransform ConvertTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var trans = new CompressedLocalTransform();
            trans.Position = position.ToFixedPointVector3();
            trans.Rotation = rotation.ToCompressedQuaternion();
            trans.Scale = scale.ToFixedPointVector3();

            return trans;
        }

        public static CompressedLocalTransform ConvertTransform(Transform trans)
        {
            if (trans == null)
                return ConvertTransform(Vector3.zero, Quaternion.identity, Vector3.one);

            return ConvertTransform(trans.position, trans.rotation, trans.localScale);
        }

        public static CompressedLocalTransform ConvertTransform(Coordinates position, CompressedQuaternion rotation, FixedPointVector3 scale)
        {
            var trans = new CompressedLocalTransform();
            trans.Position = position.ToFixedPointVector3();
            trans.Rotation = rotation;
            trans.Scale = scale;

            return trans;
        }

        public static CompressedLocalTransform ConvertTransform(Coordinates position, CompressedQuaternion? rotation, FixedPointVector3? scale)
        {
            rotation = rotation ?? TransformUtils.ToAngleAxis(0,Vector3.up);
            scale = scale ?? TransformUtils.OneVector;

            return ConvertTransform(position, rotation.Value, scale.Value);
        }

        public static float RotFoward(Vector3 targetVector)
        {
            if (targetVector == Vector3.zero)
                return 0;

            targetVector = new Vector3(targetVector.x, 0, targetVector.z);
            return Vector3.Angle(Vector3.forward, targetVector.normalized);
        }
    }
}
