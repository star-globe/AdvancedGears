using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Snapshot = Improbable.Gdk.Core.Snapshot;
using AdvancedGears;

namespace AdvancedGears.Editor
{
    [RequireComponent(typeof(MultiAimConstraint))]
    public class AimLineRenderer : MonoBehaviour
    {
        [SerializeField]
        LineRenderer line;
    
        [SerializeField]
        float length = 100.0f;
    
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
    
        void Update()
        {
            if (line == null)
                return;
    
            if (this.AimConstraint == null || this.ConstrainedTransform == null)
                return;

            points[0] = this.ConstrainedTransform.position;
            points[1] = this.ConstrainedTransform.rotation * this.AimConstraint.data.aimAxis * length + points[0];
    
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
