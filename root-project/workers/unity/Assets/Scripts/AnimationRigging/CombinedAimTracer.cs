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
                    cnt.SetOffset(aimTargetOffset.transform, vec);

                isSetOffset = true;
            }

            foreach(var cnt in controllers)
                cnt.SetTargetPosition(target);
        }

        public void Rotate(float time)
        {
            foreach(var cnt in controllers)
                cnt.Rotate(time);
        }
    }
}
