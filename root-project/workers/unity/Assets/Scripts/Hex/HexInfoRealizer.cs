using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using AdvancedGears.UI;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class HexInfoRealizer : WorldInfoReader
    {
        [Require] World world;
        protected override World World => world;

        [Require] HexBaseReader reader;
        [Require] HexPowerReader powerReader;
        [SerializeField] LandLineRenderer line;
        [SerializeField] int cutNumber = 5;
        [SerializeField] float height = 100.0f;
        [SerializeField] HexInfoText text;

        Vector3 origin;
        UnitSide currentSide;
        readonly Dictionary<UnitSide, float> currentPowers = new Dictionary<UnitSide, float>();
        IntervalCounter inter;
        readonly Vector3[] corners = new Vector3[7];

        const float diffMin = 0.1f;

        private void OnEnable()
        {
            reader.OnSideUpdate += UpdateSide;
            powerReader.OnSidePowersUpdate += UpdatePower;
        }

        private void Awake()
        {
            inter = new IntervalCounter(5);
        }

        void Start()
        {
            currentSide = reader.Data.Side;
            SetLandLine(this.Origin, reader.Data.Side, powerReader.Data.SidePowers);
        }

        private void Update()
        {
            if (inter.Check() == false)
                return;

            UpdatePower(powerReader.Data.SidePowers);
        }

        void UpdateSide(UnitSide side)
        {
            if (side == currentSide)
                return;

            currentSide = side;
            SetPowers(powerReader.Data.SidePowers);
            SetLandLine(this.Origin, side, powerReader.Data.SidePowers);
        }

        void SetPowers(Dictionary<UnitSide, float> powers)
        {
            currentPowers.Clear();
            foreach (var kvp in powers)
                currentPowers[kvp.Key] = kvp.Value;
        }

        void UpdatePower(Dictionary<UnitSide,float> powers)
        {
            if (PowerChanged(powers)) {
                SetPowers(powers);
                SetLandLine(this.Origin, currentSide, powers);
            }
        }

        private bool PowerChanged(Dictionary<UnitSide, float> powers)
        {
            if (currentPowers.Count != powers.Count)
                return true;

            foreach (var k in powers.Keys) {
                if (currentPowers.TryGetValue(k, out var p) == false)
                    return true;

                var diff = p - powers[k];
                if (diff * diff > diffMin * diffMin)
                    return true;
            }

            return false;
        }

        void SetLandLine(Vector3 origin, UnitSide side, Dictionary<UnitSide,float> powers)
        {
            if (line == null || reader == null)
                return;

            var index = reader.Data.Index;
            HexUtils.SetHexCorners(origin, index, this.corners, HexDictionary.HexEdgeLength);

            line.SetLines(side, corners, PhysicsUtils.GroundMask, cutNumber, origin.y + height, origin.y, height/100);

            text?.SetHexInfo(reader.Data.Index, reader.Data.HexId, side, powers);
        }
    }
}
