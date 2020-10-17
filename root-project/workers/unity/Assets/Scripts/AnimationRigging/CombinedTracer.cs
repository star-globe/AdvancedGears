using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class CombinedTracer : MonoBehaviour
    {
        [SerializeField]
        List<AimSpeedController> controllers = null;

        [SerializeField]
        Transform targetTransform;

        private void FixedUpdate()
        {
            Vector3? target = null;
            if (targetTransform != null)
            {
                target = targetTransform.position;
            }

            foreach(var cnt in controllers) {
                cnt.SetTargetPosition(target);
            }
        }
    }
}
