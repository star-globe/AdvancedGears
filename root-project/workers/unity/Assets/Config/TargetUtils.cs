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
        public static TargetInfo DefaultTargetInfo()
        {
            var target = new TargetInfo();
            return target;
        }

        public static TargetInfoSet DefaultTargteInfoSet()
        {
            var target = new TargetInfoSet();
            target.Type = TargetType.None;
            target.HexInfo = DefaultTargetHexInfo();
            target.FrontLine = DefaultTargetFrontLineInfo();
            target.Stronghold = DefaultTargetStrongholdInfo();
            return target;
        }

        public static TargetHexInfo DefaultTargetHexInfo()
        {
            var hex = new TargetHexInfo();
            hex.HexIndex = uint.MaxValue;
            return hex;
        }

        public static TargetFrontLineInfo DefaultTargetFrontLineInfo()
        {
            var line = new TargetFrontLineInfo();
            line.FrontLine.RightCorner = Coordinates.Zero;
            line.FrontLine.LeftCorner = Coordinates.Zero;
            return line;
        }

        public static TargetStrongholdInfo DefaultTargetStrongholdInfo()
        {
            var stronghold = new TargetStrongholdInfo();
            stronghold.Position = Coordinates.Zero;
            stronghold.Side = UnitSide.None;
            stronghold.StrongholdId = new EntityId();
            return stronghold;
        }
    }
}
