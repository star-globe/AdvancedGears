using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;

namespace AdvancedGears
{
    public class HexInfoRealizer : WorldInfoReader
    {
        [Require] World world;
        protected override World World => world;

        [Require] HexBaseReader reader;
        [SerializeField] LandLineRenderer line;
        [SerializeField] int cutNumber = 5;
        [SerializeField] float height = 100.0f;

        Vector3 origin;

        void Start()
        {
            SetLandLine(this.Origin);
        }

        void SetLandLine(Vector3 origin)
        {
            if (line != null)
            {
                var index = reader.Data.Index;
                Vector3[] corners = new Vector3[7];
                HexUtils.SetHexCorners(origin, index, corners, HexDictionary.HexEdgeLength);

                line.SetLines(corners, PhysicsUtils.GroundMask, cutNumber, origin.y + height, origin.y, height/100);
            }
        }

    }
}
