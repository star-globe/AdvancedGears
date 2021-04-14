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
            if (isSetOffset == false && aimTargetOffset != null) {
                var vec = aimTargetOffset.AimOffsetVector;
                foreach(var cnt in controllers)
                    cnt.SetOffset(this.transform, aimTargetOffset.transform, vec);
            
                isSetOffset = true;
            }

            if (target != null) {
                //this.transform.ma
                //var rot = this.transform.rotation;
                //var normalizedScale = this.transform.lossyScale.normalized;
                //var pos = Matrix4x4.Scale(normalizedScale) * this.transform.InverseTransformPoint(target.Value);
                target = this.transform.InverseTransformPoint(target.Value) + this.transform.position; //this.transform.TransformPoint(pos);//rot * this.transform.InverseTransformPoint(target.Value) + this.transform.position;//this.transform.position;//root.InverseTransformPoint(this.transform.position) + root.position;
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
