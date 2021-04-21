using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using UnityEngine.Animations.Rigging;


namespace AdvancedGears
{
    [RequireComponent(typeof(MultiAimConstraint))]
    public class AimLineRenderer : MonoBehaviour
    {
        [SerializeField]
        LineRenderer line;
    
        [SerializeField]
        float length = 5.0f;

        [SerializeField]
        float width = 0.1f;

        [SerializeField]
        UnityEngine.Color col = UnityEngine.Color.white;

        MultiAimConstraint aimConstraint;
        MultiAimConstraint AimConstraint
        {
            get
            {
                aimConstraint = aimConstraint ?? GetComponent<MultiAimConstraint>();
                return aimConstraint;
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

        readonly Vector3[] points = new Vector3[2];

        private void Start()
        {
            if (line != null)
            {
                line.startWidth = width;
                line.endWidth = width;
                line.startColor = col;
                line.endColor = col;
            }
        }

        void Update()
        {
            if (line == null)
                return;
    
            if (this.AimConstraint == null || this.ConstrainedTransform == null)
                return;

            points[0] = this.ConstrainedTransform.position;
            points[1] = AnimationRiggingUtils.GetAimAxis(ref this.AimConstraint.data) * length + points[0];
    
            bool changed = false;
            var count = line.positionCount;
            if (count == points.Length)
            {
                for (int i = 0; i < count; i++)
                {
                    changed |= points[i] != line.GetPosition(i);
                }
            }
            else
                changed = true;
    
            if (changed == false)
                return;
    
            line.SetPositions(points);
            line.positionCount = points.Length;
        }
    }
}
