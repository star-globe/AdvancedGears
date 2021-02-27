using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/UnitCommonSettings", order = 0)]
    public class UnitCommonSettings : ScriptableObject
    {
        public UnitType type;
        public bool isBuilding;
        public bool isAutomaticallyMoving;
        public bool isOfficer;
        public bool isOffecsive;
        public bool isWatcher;
    }
}
