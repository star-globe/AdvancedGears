using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    public class BaseUnitAnimator : MonoBehaviour
    {
        [Require] BaseUnitStatusReader status;

        [SerializeField]
        Animator animator;

        private void OnEnable()
        {
            SwitchAnimator(status.Data.State);
            status.OnStateUpdate += SwitchAnimator;
        }

        private void SwitchAnimator(UnitState state)
        {
            bool isEnable = state == UnitState.Alive;

            if (animator != null && animator.enabled != isEnable)
                animator.enabled = isEnable;
        }
    }
}
