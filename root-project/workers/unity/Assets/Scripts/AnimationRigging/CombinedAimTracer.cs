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
                target = this.transform.rotation * this.transform.InverseTransformPoint(target.Value) + this.transform.position;
                // var pos = this.transform.position;
                // var rot = this.transform.rotation;
                // var scale = this.transform.lossyScale.magnitude;
                //Matrix4x4 m = Matrix4x4.TRS(pos, rot, scale * Vector3.one);
                // target = m.MultiplyPoint(this.transform.InverseTransformPoint(target.Value));
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
