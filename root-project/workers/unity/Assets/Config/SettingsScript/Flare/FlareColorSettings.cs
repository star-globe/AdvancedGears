using System;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using AdvancedGears.UI;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Flare Config/FlareColorSettings", order = 1)]
    public class FlareColorSettings : ScriptableObject
    {
        [Serializable]
        class FlareColorPair
        {
            public FlareColorType flareColorType;
            public ParticleSystem.MinMaxGradient colorGradiet;
        }

        [SerializeField]
        FlareColorPair[] pairs;

        public void SetColor(ParticleSystem.MainModule main, FlareColorType flareType)
        {
            foreach (var p in pairs)
            {
                if (p.flareColorType != flareType)
                    continue;

                main.startColor = p.colorGradiet;
                return;
            }
        }
    }
}
