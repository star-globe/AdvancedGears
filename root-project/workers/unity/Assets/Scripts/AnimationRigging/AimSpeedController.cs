using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace AdvancedGears
{
    [RequireComponent(typeof(MultiAimConstraint))]
    public class AimSpeedController : MonoBehaviour
    {
        MultiAimConstraint aimConstraint;
        MultiAimConstraint AimConstraint
        {
            get
            {
                aimConstraint = aimConstraint ?? GetComponent<MultiAimConstraint>();
                return aimConstraint;
            }
        }

        Transform sourceTransform;
        Transform SourceTransform
        {
            get
            {
                if (sourceTransform == null) {
                    if (this.AimConstraint != null &&
                        this.AimConstraint.data.sourceObjects.Count > 0) {
                        sourceTransform = this.AimConstraint.data.sourceObjects[0].transform;
                    }
                }

                return sourceTransform;
            }
        }

        Transform constrainedTransform;
        Transform ConstrainedTransform
        {
            get
            {
                if (constrainedTransform == null)
                {
                    if (this.AimConstraint != null)
                    {
                        constrainedTransform = this.AimConstraint.data.constrainedObject;
                    }
                }
                return constrainedTransform;
            }
        }

        Vector3? axisVector = null;
        Vector3? AxisVector
        {
            get
            {
                if (axisVector == null) {
                    if (this.AimConstraint != null) {
                        axisVector = AnimationRiggingUtils.GetRotAxis(this.AimConstraint);
                    }
                }
                return axisVector;
            }
        }

        Vector3? target = null;
        float speed = 0.0f;

        [SerializeField]
        float rotSpeed;

        private void Start()
        {
            SetRotSpeed(rotSpeed);
        }

        public void SetRotSpeed(float speed)
        {
            this.speed = speed;
        }

        public void Rotate(float time)
        {
            if (target == null)
                return;

            if (this.SourceTransform == null)
                return;

            if (this.AxisVector == null)
                return;

            var baseTgt = this.ConstrainedTransform.InverseTransformPoint(target.Value);
            var baseSource = this.ConstrainedTransform.InverseTransformPoint(this.SourceTransform.position);

            var tgt = Vector3.ProjectOnPlane(baseTgt, axisVector.Value);
            var source = Vector3.ProjectOnPlane(baseSource, axisVector.Value);

            var sourceRadius = source.magnitude;

            tgt.Normalize();
            source.Normalize();

            var diff = tgt - source;
            if (diff.sqrMagnitude < 0.01f * 0.01f)
                return;

            var axis = Vector3.Cross(source, tgt);
            int sign = Vector3.Dot(axis, AxisVector.Value) > 0 ? 1: -1;

            var rot = Vector3.Cross(axisVector.Value, source);

            var vector = rot * sign * this.speed * time * Mathf.Deg2Rad;

            baseSource += vector * sourceRadius;

            this.SourceTransform.position = this.ConstrainedTransform.TransformPoint(baseSource);
        }

        public void SetTargetPosition(Vector3? target)
        {
            this.target = target;
        }
    }
}
