using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
    public class ConnectorTransform : AttachedTransform
    {
        [SerializeField] AttachedTransform attached;
        public AttachedTransform Attached { get { return attached; } }

        void Start()
        {
            Assert.IsNotNull(attached);
        }

        public void SetAttach(AttachedTransform trans)
        {
            attached = trans;
        }

        public override Vector3 TargetTargetVector
        {
            get { return attached.transform.position - this.transform.position; }
        }
    }
}

