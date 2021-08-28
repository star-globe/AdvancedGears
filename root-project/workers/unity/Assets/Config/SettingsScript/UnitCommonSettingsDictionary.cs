using System;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/UnitCommonSettings Dictionary", order = 0)]
    public class UnitCommonSettingsDictionary : DictionarySettings
    {
        public static UnitCommonSettingsDictionary Instance { private get; set; }

        [SerializeField] private UnitCommonSettings[] settgins;

        Dictionary<UnitType, UnitCommonSettings> settingsDic = null;
        Dictionary<UnitType, UnitCommonSettings> SettingsDic
        {
            get
            {
                if (settingsDic == null) {
                    settingsDic = new Dictionary<UnitType, UnitCommonSettings>();

                    foreach (var set in settgins)
                    {
                        if (settingsDic.ContainsKey(set.type))
                            continue;

                       settingsDic.Add(set.type, set);
                    }
                }

                return settingsDic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public UnitCommonSettings GetUnitSettings(UnitType unitType)
        {
            if (SettingsDic.TryGetValue(unitType, out var set))
                return set;

            return null;
        }

        public static UnitCommonSettings GetSettings(UnitType unitType)
        {
            if (Instance.SettingsDic.TryGetValue(unitType, out var set))
                return set;

            return null;
        }

        public static Dictionary<UnitType, UnitCommonSettings> Dic
        {
            get { return Instance.SettingsDic; }
        }
    }
}
