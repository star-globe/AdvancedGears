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

        public static bool IsOffensive(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isOffecsive;
        }

        public static bool IsWatcher(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isWatcher;
        }

        public static bool IsOfficer(UnitType type)
        {
            var set = UnitCommonSettingsDictionary.GetSettings(type);
            return set != null && set.isOfficer;
        }

        public static bool IsNeedUpdate(int bitNumber, TargetType type)
        {
            return (bitNumber & 1 << (int)type) != 0;
        }

        public static int UpdateTargetBit(int bitNumber, TargetType type)
        {
            return bitNumber + (1 << (int)type);
        }
    }
}
