using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    public abstract class BaseColorSettings : ScriptableObject
    {
        readonly Dictionary<int, Dictionary<uint, UnityEngine.Color>> colorSettingDic = new Dictionary<int, Dictionary<uint, UnityEngine.Color>>();
        readonly Dictionary<Type, List<IColor>> typeColorListDic = new Dictionary<Type, List<IColor>>();

        protected UnityEngine.Color GetColor(List<IColor> list, uint key)
        {
            if (list == null)
                return UnityEngine.Color.white;

            var hash = list.GetHashCode();
            if (colorSettingDic.TryGetValue(hash, out var dic) == false) {
                dic = new Dictionary<uint, UnityEngine.Color>();
                foreach (var c in list)
                    dic.Add(c.Key, c.Color);

                colorSettingDic.Add(hash,dic);
            }

            if (dic.TryGetValue(key, out var col))
                return col;
            else
                return UnityEngine.Color.white;
        }

        protected List<IColor> ConvertToColorList(Type type, IEnumerable<IColor> baseColors)
        {
            List<IColor> list = new List<IColor>();
            foreach (var c in baseColors)
                list.Add(c);

            typeColorListDic[type] = list;
            
            return list;
        }

        protected List<IColor> GetColorList(Type type)
        {
            List<IColor> list = null;
            typeColorListDic.TryGetValue(type, out list);
            return list;
        }
    }

    [Serializable]
    abstract class BaseColor
    {
        [SerializeField]
        UnityEngine.Color col;

        public UnityEngine.Color Color { get { return col; } }
    }

    public interface IColor
    {
        uint Key { get; }
        UnityEngine.Color Color { get; }
    }
}
