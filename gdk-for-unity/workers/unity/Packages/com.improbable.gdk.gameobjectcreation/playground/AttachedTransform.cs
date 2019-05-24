using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
    public abstract class AttachedTransform : MonoBehaviour
    {
        public enum VectorType
        {
            Locked = 0,
            Up,
            Forward,
            Right,
        }

        [SerializeField] VectorType hingeVectorType;

        Vector3 getVecor(VectorType type)
        {
            switch (type)
            {
                case VectorType.Up:         return this.transform.up;
                case VectorType.Forward:    return this.transform.forward;
                case VectorType.Right:      return this.transform.right;

                default: return Vector3.zero;
            }
        }

        public Vector3 HingeAxis
        {
            get { return getVecor(hingeVectorType); }
        }

        public abstract Vector3 TargetVector { get; }
    }
}

