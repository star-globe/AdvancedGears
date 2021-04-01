using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class AimTargetOffset : MonoBehaviour
    {
        [SerializeField]
        Transform aimTargetTrans;

        Transform currentTrans = null;
        Transform CurrentTrans
        {
            get
            {
                currentTrans = currentTrans ?? this.transform;
                return currentTrans;
            }
        }

        public Vector3 AimOffsetVector
        {
            get
            {
                if (aimTargetTrans == null || this.CurrentTrans == null)
                    return Vector3.zero;

                var diff = aimTargetTrans.position - this.CurrentTrans.position;
                return this.CurrentTrans.InverseTransformVector(diff);
            }
        }
    }
}
