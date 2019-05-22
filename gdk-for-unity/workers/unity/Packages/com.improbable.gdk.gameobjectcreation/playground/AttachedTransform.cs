using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
    public abstract class AttachedTransform : MonoBehaviour
    {
        public enum HingeVectorType
        {
            Locked = 0,
            Up,
            Forward,
            Right,
        }

        [SerializeField] HingeVectorType hingeVectorType;

        public Vector3 HingeAxis
        {
            get
            {
                switch(hingeVectorType)
                {
                    case HingeVectorType.Up:        return this.transform.up;
                    case HingeVectorType.Forward:   return this.transform.forward;
                    case HingeVectorType.Right:     return this.transform.right;

                    default:    return Vector3.zero;
                }
            }
        }
    }
}

