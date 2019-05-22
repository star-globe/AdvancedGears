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
            Down,
            Forward,
            Back,
            Right,
            Left,
        }

        public enum HingeVectorType
        {
            Locked = VectorType.Locked,
            Up = VectorType.Up,
            Foward = VectorType.Forward,
            Right = VectorType.Right,
        }

        [SerializeField] HingeVectorType hingeVectorType;
        [SerializeField] VectorType fowardVectorType;

        Vector3 getVecor(VectorType type)
        {
            switch (type)
            {
                case VectorType.Up:         return this.transform.up;
                case VectorType.Down:       return -this.transform.up;
                case VectorType.Forward:    return this.transform.forward;
                case VectorType.Back:       return -this.transform.forward;
                case VectorType.Right:      return this.transform.right;
                case VectorType.Left:       return -transform.right;

                default: return Vector3.zero;
            }
        }

        public Vector3 HingeAxis
        {
            get { return getVecor((VectorType)hingeVectorType); }
        }

        public Vector3 BoneFoward
        {
            get { return getVecor(fowardVectorType); }
        }
    }
}

