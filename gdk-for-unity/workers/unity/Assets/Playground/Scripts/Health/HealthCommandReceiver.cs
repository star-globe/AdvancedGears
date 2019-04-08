using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class HealthCommmandReceiver : MonoBehaviour
    {
        [Require] BaseUnitHealthCommandReceiver healthCommandReceiver;
        [Require] BaseUnitHealthWriter healthWriter;
        [Require] BaseUnitStatusWriter statusWriter;

        public void OnEnable()
        {
            healthCommandReceiver.OnModifyHealthRequestReceived += OnModifiedHealthRequest;
        }

        private void OnModifiedHealthRequest(BaseUnitHealth.ModifyHealth.ReceivedRequest request)
        {
            healthCommandReceiver.SendModifyHealthResponse(new BaseUnitHealth.ModifyHealth.Response(request.RequestId, new Empty()));

            var max = healthWriter.Data.MaxHealth;

            var health = request.Payload.Amount;
            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health,
            });

            if (health == 0)
            {
                statusWriter.SendUpdate(new BaseUnitStatus.Update()
                {
                    State = UnitState.Dead,
                });
            }
        }
    }
}
