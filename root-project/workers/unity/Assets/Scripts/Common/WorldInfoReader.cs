using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public abstract class WorldInfoReader : MonoBehaviour
    {
        protected abstract World World { get;}

        WorkerSystem worker = null;
        public WorkerSystem Worker
        {
            get
            {
                worker = worker ?? this.World?.GetExistingSystem<WorkerSystem>();
                return worker;
            }
        }

        Vector3? origin = null;
        public Vector3 Origin
        {
            get
            {
                if (origin == null) {
                    if (this.Worker != null)
                        origin = this.Worker.Origin;
                    else
                        return Vector3.zero;
                }


                return origin.Value;
            }
        }
    }
}
