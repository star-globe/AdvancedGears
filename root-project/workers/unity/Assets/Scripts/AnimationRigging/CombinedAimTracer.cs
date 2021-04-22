using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class CombinedAimTracer : MonoBehaviour
    {
        [SerializeField]
        List<AimSpeedController> controllers = null;

        [SerializeField]
        AimTargetOffset aimTargetOffset = null;

        bool isSetOffset = false;

        public void SetAimTarget(Vector3? target, Transform centerTrans = null)
        {
            //if (isSetOffset == false && aimTargetOffset != null) {
            //    var vec = aimTargetOffset.AimOffsetVector;
            //    foreach(var cnt in controllers)
            //        cnt.SetOffset(this.transform, aimTargetOffset.transform, vec);
            //
            //    isSetOffset = true;
            //}

            if (target != null) {
#if false
                target = this.transform.rotation * this.transform.InverseTransformPoint(target.Value) + this.transform.position;
#elif false
                target = this.transform.InverseTransformPoint(target.Value) + this.transform.position;
#elif false
                var rot = Matrix4x4.Rotate(this.transform.rotation);
                var scale = Matrix4x4.Scale(AnimationRiggingUtils.GetScaleRate(this.transform.lossyScale) * Vector3.one);
                target = (rot * scale).MultiplyPoint3x4(this.transform.InverseTransformPoint(target.Value)) + this.transform.position;
#elif false
                var diff = target.Value - this.transform.position;
                var scale = Matrix4x4.Scale(AnimationRiggingUtils.NormilizedScaleVector(this.transform.lossyScale));
                target = scale.MultiplyPoint3x4(diff) + this.transform.position;
#elif false
                var scale = Matrix4x4.Scale(AnimationRiggingUtils.GetScaleRate(this.transform.lossyScale) * Vector3.one);
                target = scale.MultiplyPoint3x4(this.transform.InverseTransformPoint(target.Value)) + this.transform.position;
#elif false
                var rot = Matrix4x4.Rotate(this.transform.rotation);
                var scale = Matrix4x4.Scale(this.transform.lossyScale);
                target = (rot * scale).MultiplyPoint3x4(this.transform.InverseTransformPoint(target.Value)) + this.transform;
#endif
            }

            foreach (var cnt in controllers)
                cnt.SetTargetPosition(target);
        }

        Vector3 before;

        public void Rotate(float time)
        {
            foreach(var cnt in controllers)
                cnt.Rotate(time);
        }
    }
}
