using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class SpatialMonoBehaviour : MonoBehaviour
    {
        LinkedEntityComponent spatialComp = null;
        protected LinkedEntityComponent SpatialComp
        {
            get
            {
                if (spatialComp == null)
                    spatialComp = GetComponent<LinkedEntityComponent>();

                return spatialComp;
            }
        }

        protected EntityManager? EntityManager
        {
            get { return SpatialComp?.World?.EntityManager; }
        }

        protected WorkerSystem Worker
        {
            get { return SpatialComp?.Worker; }
        }
    }
}
