using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace AdvancedGears
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
        public VectorType HingeVectorType => hingeVectorType;

        protected Vector3 getVector(VectorType type)
        {
            switch (type)
            {
                case VectorType.Up: return this.transform.up;
                case VectorType.Forward: return this.transform.forward;
                case VectorType.Right: return this.transform.right;

                default: return Vector3.zero;
            }
        }

        public Vector3 HingeAxis
        {
            get { return getVector(hingeVectorType); }
        }

        public virtual ConnectorConstrain Constrain { get { return null; } }
    }
}

