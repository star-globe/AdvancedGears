using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class HealthCommandReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] BaseUnitHealthCommandReceiver healthCommandReceiver;
        [Require] BaseUnitHealthWriter healthWriter;
        [Require] BaseUnitStatusWriter statusWriter;

        LinkedEntityComponent spatialComp = null;
        LinkedEntityComponent SpatialComp
        {
            get
            {
                if (spatialComp == null)
                    spatialComp = GetComponent<LinkedEntityComponent>();

                return spatialComp;
            }
        }

        BaseUnitReviveTimerSystem timerSystem;

        public void OnEnable()
        {
            healthCommandReceiver.OnModifyHealthRequestReceived += OnModifiedHealthRequest;
            timerSystem = world.GetExistingSystem<BaseUnitReviveTimerSystem>();
        }

        private void OnModifiedHealthRequest(BaseUnitHealth.ModifyHealth.ReceivedRequest request)
        {
            healthCommandReceiver.SendModifyHealthResponse(new BaseUnitHealth.ModifyHealth.Response(request.RequestId, new Empty()));

            var health = request.Payload.Amount;
            if (CheckAndUpdateHealth(health) == false)
                return;

            if (health > 0)
                return;

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                State = UnitState.Dead,
            });

            timerSystem?.AddDeadUnit(SpatialComp.EntityId);
        }

        private bool CheckAndUpdateHealth(int health)
        {
            var current= healthWriter.Data.Health;

            if (current == health)
                return false;

            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health,
            });

            return true;
        }
    }
}
