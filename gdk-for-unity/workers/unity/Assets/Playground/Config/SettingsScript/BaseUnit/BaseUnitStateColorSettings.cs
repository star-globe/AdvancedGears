using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/StateColorSettings", order = 1)]
    public class BaseUnitStateColorSettings : ScriptableObject
    {
        [SerializeField]
        StateColor[] stateColors;

        [SerializeField]
        SideColor[] sideColors;

        UnityEngine.Color GetColor<T>(IEnumerable<IColor<T>> list, T tgt) where T : struct
        {
            var col = UnityEngine.Color.white;
            var bCol = list.FirstOrDefault(c => c.Tgt.Equals(tgt));
            if (bCol != null)
                col = bCol.Color;

            return col;
        }

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
    abstract class BaseColor
    {
        [SerializeField]
        UnityEngine.Color col;

        public UnityEngine.Color Color { get { return col; } }
    }

    interface IColor<T> where T : struct
    {
        T Tgt { get; }
        UnityEngine.Color Color { get; }
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
