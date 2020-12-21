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

        public UnityEngine.Color GetStateColor(UnitState state)
        {
            return GetColor(stateColors,state);
        }

        public UnityEngine.Color GetSideColor(UnitSide side)
        {
            return GetColor(sideColors,side);
        }
    }

    [Serializable]
    internal class StateColor : BaseColor, IColor<UnitState>
    {
        public UnitState Tgt => state;

        [SerializeField]
        UnitState state;
    }

    [Serializable]
    internal class SideColor : BaseColor, IColor<UnitSide>
    {
        public UnitSide Tgt => side;

        [SerializeField]
        UnitSide side;
    }
}
