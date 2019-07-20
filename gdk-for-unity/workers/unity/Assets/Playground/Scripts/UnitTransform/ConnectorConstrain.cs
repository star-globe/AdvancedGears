using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    public class ConnectorConstrain : AttachedTransform
    {
        [SerializeField] VectorType orderVectorType;

        [SerializeField] float min;
        public float Min => Inverse ? -max: min;

        [SerializeField] float max;
        public float Max => Inverse ? -min: max;

        bool Inverse
        {
            get
            {
                int n = (int)this.HingeVectorType;
                int o = (int)this.orderVectorType;

                if (n == o)
                    return false;

                return n > o;
            }
        }

        public Vector3 OrderVector
        {
            get
            {
                return getVector(orderVectorType);
            }
        }

    }
}
