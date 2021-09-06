using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class PlayerReviveLogicCommandReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] PlayerRespawnReader reader;
        [Require] BaseUnitStatusWriter statusWriter;
        [Require] BaseUnitHealthWriter healthWriter;

        private void OnEnable()
        {
            reader.OnRespawnEvent += OnRespawn;
        }

        private void OnRespawn(Empty empty)
        {
            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                State = UnitState.Alive,
            });

            var max = healthWriter.Data.MaxHealth;

            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = max,
            });
        }
    }
}
