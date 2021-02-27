using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;
using AdvancedGears.UI;

namespace AdvancedGears
{
    public static class TargetUtils
    {
        public static UnitBaseInfo DefaultTargetInfo()
        {
            var unit = new UnitBaseInfo();
            unit.Position = Coordinates.Zero;
            unit.Side = UnitSide.None;
            unit.UnitId = new EntityId();
            return unit;
        }

        public static TargetInfoSet DefaultTargteInfoSet()
        {
            var target = new TargetInfoSet();
            target.HexInfo = DefaultTargetHexInfo();
            target.FrontLine = DefaultTargetFrontLineInfo();
            target.Stronghold = DefaultTargetInfo();
            target.PowerRate = 1.0f;
            return target;
        }

        public static HexBaseInfo DefaultTargetHexInfo()
        {
            var hex = new HexBaseInfo();
            hex.HexIndex = uint.MaxValue;
            return hex;
        }

        public static FrontLineInfo DefaultTargetFrontLineInfo()
        {
            var line = new FrontLineInfo();
            line.RightCorner = Coordinates.Zero;
            line.LeftCorner = Coordinates.Zero;
            return line;
        }

        public static int FindIndex(List<uint> list, uint id)
        {
            if (list == null)
                return -1;

            int idx = -1;
            for (int i = 0; i < list.Count; i++) {
                if (id == list[i])
                    idx = i;
            }

            return idx;
        }
    }
}
