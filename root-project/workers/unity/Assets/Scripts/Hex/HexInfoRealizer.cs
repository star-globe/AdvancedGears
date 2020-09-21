using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using TMPro;
using UnityEngine.Assertions;

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
        [SerializeField] HexInfoText text;

        Vector3 origin;
        UnitSide currentSide;

        private void OnEnable()
        {
            reader.OnSideUpdate += UpdateSide;
        }

        void Start()
        {
            if (reader == null)
                return;

            currentSide = reader.Data.Side;
            SetLandLine(this.Origin, reader.Data.Side);
        }

        void UpdateSide(UnitSide side)
        {
            if (side == currentSide)
                return;

            currentSide = side;
            SetLandLine(this.Origin, side);
        }

        void SetLandLine(Vector3 origin, UnitSide side)
        {
            if (line == null || reader == null)
                return;

            var index = reader.Data.Index;
            Vector3[] corners = new Vector3[7];
            HexUtils.SetHexCorners(origin, index, corners, HexDictionary.HexEdgeLength);

            line.SetLines(side, corners, PhysicsUtils.GroundMask, cutNumber, origin.y + height, origin.y, height/100);

            text?.SetHexInfo(reader.Data.Index, reader.Data.HexId, side);
        }
    }
}
