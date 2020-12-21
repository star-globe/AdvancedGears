using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    public abstract class BaseColorSettings : ScriptableObject
    {
        protected UnityEngine.Color GetColor<T>(IEnumerable<IColor<T>> list, T tgt) where T : struct
        {
            var col = UnityEngine.Color.white;
            var bCol = list.FirstOrDefault(c => c.Tgt.Equals(tgt));
            if (bCol != null)
                col = bCol.Color;

            return col;
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
}
