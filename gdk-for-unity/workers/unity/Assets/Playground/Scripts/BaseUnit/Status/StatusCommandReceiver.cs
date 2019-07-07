using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace Playground
{
    public class StatusCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitStatusCommandReceiver statusCommandReceiver;
        [Require] BaseUnitStatusWriter statusWriter;
        [Require] BaseUnitHealthWriter healthWriter;

        public void OnEnable()
        {
            statusCommandReceiver.OnSetOrderRequestReceived += OnSetOrderRequest;
            statusCommandReceiver.OnForceStateRequestReceived += OnForceStateRequest;
        }

        private void OnSetOrderRequest(BaseUnitStatus.SetOrder.ReceivedRequest request)
        {
            statusCommandReceiver.SendSetOrderResponse(new BaseUnitStatus.SetOrder.Response(request.RequestId, new Empty()));

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Order = request.Payload.Order,
            });
        }

        private void OnForceStateRequest(BaseUnitStatus.ForceState.ReceivedRequest request)
        {
            statusCommandReceiver.SendForceStateResponse(new BaseUnitStatus.ForceState.Response(request.RequestId, new Empty()));

            var change = request.Payload;
            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Side = change.Side,
                State = change.State,
            });

            if (change.State != UnitState.Alive)
                return;

            var health = healthWriter.Data;
            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health.MaxHealth,
            });
        }
    }
}
