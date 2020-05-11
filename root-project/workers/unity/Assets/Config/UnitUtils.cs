using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;
using AdvancedGears.UI;

namespace AdvancedGears
{
    public static class UnitUtils
    {
        public static bool IsBuilding(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isBuilding;
        }

        public static bool IsAutomaticallyMoving(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isAutomaticallyMoving;
        }

        public static bool IsOfficer(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isOfficer;
        }
    }
}
