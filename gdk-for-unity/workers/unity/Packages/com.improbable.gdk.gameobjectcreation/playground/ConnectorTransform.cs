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

        public void SetAttach(AttachedTransform trans)
        {
            attachedTrans = trans;
        }
    }
}

