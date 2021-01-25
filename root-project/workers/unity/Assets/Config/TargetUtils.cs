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
            unit.StrongholdId = new EntityId();
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

        public static HexInfo DefaultTargetHexInfo()
        {
            var hex = new HexInfo();
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
    }
}
