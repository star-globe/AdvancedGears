using UnityEngine;
using UnityEngine.UI;

namespace AdvancedGears.UI
{
    public static class UIUtils
    {
        public static string GetName(this UnitSide side)
        {
            switch (side)
            {
                case UnitSide.A:
                    return "A";

                case UnitSide.B:
                    return "B";

                case UnitSide.C:
                    return "C";
            }

            return string.Empty;
        }
    }

    public enum UIType
    {
        None = 0,

        // Battle
        HeadStatus = 100,

        // MiniMap
        MiniMapDisplay = 200,
        MiniMapObject,
    }
}
