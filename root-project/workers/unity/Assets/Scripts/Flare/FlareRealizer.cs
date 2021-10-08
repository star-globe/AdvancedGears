using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using Unity.Entities;
using UnityEngine;
using AdvancedGears.UI;
using UnityEngine.Assertions;

namespace AdvancedGears
{
    public class FlareRealizer : MonoBehaviour
    {
        [Require] StrategyFlareReader reader;

        [SerializeField]
        ParticleSystem particle;

        [SerializeField]
        FlareColorSettings settings;

        private void Start()
        {
            settings.SetColor(particle.main, reader.Data.Color);
            particle.Play();
        }
    }
}
