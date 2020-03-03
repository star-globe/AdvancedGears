using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class VirtualCommandReceiver : MonoBehaviour
    {
        [Require] VirtualTroopWriter writer;
        [Require] CommanderStatusReader reader;
        [Require] BaseUnitStatusReader statusReader;

        public void OnEnable()
        {
            writer.OnTotalHealthDiffEvent += OnDiffedEvent;
        }

        private void OnDiffedEvent(TotalHealthDiff diff)
        {
            var container = writer.Data.TroopContainer;
            var simpleUnits = container.SimpleUnits;
            var count = simpleUnits.Count;
            if (count == 0)
                return;

            var perDamage = diff.HealthDiff / count;
            var keys = simpleUnits.Keys;
            foreach (var k in keys) {
                var simple = simpleUnits[k];
                simple.Health = Mathf.Max(0, simple.Health - perDamage);
                simpleUnits[k] = simple;
            }

            container.SimpleUnits = simpleUnits;

            writer.SendUpdate(new VirtualTroop.Update()
            {
                TroopContainer = container,
            });
        }
    }
}
