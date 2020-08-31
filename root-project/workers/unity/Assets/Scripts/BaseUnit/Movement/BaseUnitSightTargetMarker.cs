using System.Collections;
using Unity.Entities;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;

namespace AdvancedGears
{
    public class BaseUnitSightTargetMarker : WorldInfoReader
    {
        [Require] World world;
        protected override World World => world;

        [Require] BaseUnitSightReader reader;
        [SerializeField] GameObject markerObject;

        void Start()
        {
            reader.OnTargetPositionUpdate += UpdatePosition;
        }

        void UpdatePosition(FixedPointVector3 pos)
        {
            if (markerObject != null)
                markerObject.transform.position = pos.ToWorkerPosition(this.Origin);
        }
    }
}
