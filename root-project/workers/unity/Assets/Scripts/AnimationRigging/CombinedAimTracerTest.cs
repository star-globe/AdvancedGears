using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class CombinedAimTracerTest : CombinedAimTracer
    {
        [SerializeField]
        Transform targetTransform;

        private void FixedUpdate()
        {
            Vector3? target = null;
            if (targetTransform != null)
            {
                target = targetTransform.position;
            }

            SetAimTarget(target);

            Rotate(Time.deltaTime);
        }
    }
}
