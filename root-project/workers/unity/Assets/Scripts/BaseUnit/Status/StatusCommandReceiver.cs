using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class StatusCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitStatusCommandReceiver statusCommandReceiver;
        [Require] BaseUnitStatusWriter statusWriter;
        [Require] BaseUnitHealthWriter healthWriter;

        public void OnEnable()
        {
            statusCommandReceiver.OnSetOrderRequestReceived += OnSetOrderRequest;
            statusWriter.OnForceStateEvent += OnForceState;
        }

        private void OnSetOrderRequest(BaseUnitStatus.SetOrder.ReceivedRequest request)
        {
            statusCommandReceiver.SendSetOrderResponse(new BaseUnitStatus.SetOrder.Response(request.RequestId, new Empty()));

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Order = request.Payload.Order,
                Rate = request.Payload.Rate,
            });
        }

        private void OnForceState(ForceStateChange change)
        {
            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Side = change.Side,
                State = change.State,
            });

            switch(change.State)
            {
                case UnitState.Alive:   Revive(); break;
                case UnitState.Sleep:   SetSleep(); break;
            }
        }

        private void Revive()
        {
            var health = healthWriter.Data;
            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health.MaxHealth,
                RecoveryAmount = 0,
            });
        }

        private void SetSleep()
        {
            var pos = this.transform.position;
            this.transform.position = new Vector3(pos.x, FixedParams.AbyssHeight, pos.z);
        }
    }
}
