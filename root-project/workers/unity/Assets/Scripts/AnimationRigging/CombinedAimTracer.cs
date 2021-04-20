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

        public void SetAimTarget(Vector3? target)
        {
            //if (isSetOffset == false && aimTargetOffset != null) {
            //    var vec = aimTargetOffset.AimOffsetVector;
            //    foreach(var cnt in controllers)
            //        cnt.SetOffset(this.transform, aimTargetOffset.transform, vec);
            //
            //    isSetOffset = true;
            //}

            if (target != null) {
#if true
                target = this.transform.rotation * this.transform.InverseTransformPoint(target.Value) + this.transform.position;
#elif false
                target = this.transform.InverseTransformPoint(target.Value) + this.transform.position;
#elif true
                var rot = Matrix4x4.Rotate(this.transform.rotation);
                var scale = Matrix4x4.Scale(AnimationRiggingUtils.GetScaleRate(this.transform.lossyScale) * Vector3.one);
                target = (rot * scale).MultiplyPoint3x4(this.transform.InverseTransformPoint(target.Value)) + this.transform.position;
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
