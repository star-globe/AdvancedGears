using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/StateColorSettings", order = 1)]
    public class BaseUnitStateColorSettings : BaseColorSettings
    {
        [SerializeField]
        StateColor[] stateColors;

        [SerializeField]
        SideColor[] sideColors;

        Type stateType = null;
        Type sideType = null;

        public UnityEngine.Color GetStateColor(UnitState state)
        {
            stateType = stateType ?? typeof(StateColor);
            var list = GetColorList(stateType);
            list = list ?? ConvertToColorList(stateType, stateColors);

            return GetColor(list, (uint)state);
        }

        public UnityEngine.Color GetSideColor(UnitSide side)
        {
            sideType = sideType ?? typeof(UnitSide);
            var list = GetColorList(sideType);
            list = list ?? ConvertToColorList(sideType, sideColors);

            return GetColor(list, (uint)side);
        }
    }

    [Serializable]
    internal class StateColor : BaseColor, IColor
    {
        public uint Key => (uint)state;

        [SerializeField]
        UnitState state;
    }

    [Serializable]
    internal class SideColor : BaseColor, IColor
    {
        public uint Key => (uint)side;

        [SerializeField]
        UnitSide side;
    }
}
