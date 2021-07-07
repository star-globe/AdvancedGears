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
        Vector3 defaultPosition = Vector3.zero;
        Vector3 TargetPosition
        {
            get
            {
                Vector3 pos = defaultPosition;
                if (target != null) {
                    pos = target.Value;
                }
                else if(this.ConstrainedTransform != null) {
                    pos = this.ConstrainedTransform.TransformPoint(pos);
                }

                if (this.ConstrainedTransform != null &&
                    offsetTrans != null && rootTrans != null &&
                    offsetVector3 != Vector3.zero) {
                    pos -= offsetTrans.TransformVector(offsetVector3) + offsetTrans.position - this.ConstrainedTransform.position;
                }

                return pos;
            }
        }

        Transform offsetTrans = null;
        Transform rootTrans = null;
        Vector3 offsetVector3 = Vector3.zero;

        float speed = 0.0f;

        [SerializeField]
        float rotSpeed;

        private void Start()
        {
            SetRotSpeed(rotSpeed);

            if (this.SourceTransform != null &&
                this.ConstrainedTransform != null)
                defaultPosition = this.ConstrainedTransform.InverseTransformPoint(this.SourceTransform.position);

            //Debug.Log($"DefaultPosition:{defaultPosition}");
        }

        public void SetRotSpeed(float speed)
        {
            this.speed = speed;
        }

        public void SetOffset(Transform root, Transform trans, Vector3 vec)
        {
            this.offsetTrans = trans;
            this.offsetVector3 = vec;
            this.rootTrans = root;
        }

        const float diffThreshhold = 0.005f;

        public void Rotate(float time)
        {
            if (this.SourceTransform == null ||
                this.ConstrainedTransform == null)
                return;

            if (this.AxisVector == null)
                return;

            var baseTgt = this.ConstrainedTransform.InverseTransformPoint(this.TargetPosition);
            var baseSource = this.ConstrainedTransform.InverseTransformPoint(this.SourceTransform.position);

            var tgt = Vector3.ProjectOnPlane(baseTgt, axisVector.Value);
            var source = Vector3.ProjectOnPlane(baseSource, axisVector.Value);

            var sourceRadius = source.magnitude;

            tgt.Normalize();
            source.Normalize();

            var diff = tgt - source;
            if (diff.sqrMagnitude < diffThreshhold * diffThreshhold)
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
