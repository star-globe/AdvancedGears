using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Playground
{
    public class ConnectorTransform : AttachedTransform
    {
        [SerializeField] ConnectorTransform parentConnector;
        public ConnectorTransform ParentConnector => parentConnector;

        [SerializeField] ConnectorConstrain selfConstrain;
        public ConnectorConstrain SlefConstrain => selfConstrain;


        public override ConnectorConstrain Constrain
        {
            get { return parentConnector?.selfConstrain; }
        }
   }
}

