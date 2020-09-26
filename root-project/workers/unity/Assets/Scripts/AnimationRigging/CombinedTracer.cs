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
            if (targetTransform == null)
                return;

            foreach(var cnt in controllers) {
                cnt.SetTargetPosition(targetTransform.position);
            }
        }
    }
}
