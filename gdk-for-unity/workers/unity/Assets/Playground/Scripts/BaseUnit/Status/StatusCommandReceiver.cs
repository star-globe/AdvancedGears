using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class StatusCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitStatusCommandReceiver statusCommandReceiver;
        [Require] BaseUnitStatusWriter statusWriter;

        public void OnEnable()
        {
            statusCommandReceiver.OnSetOrderRequestReceived += OnSetOrderRequest;
        }

        private void OnSetOrderRequest(BaseUnitStatus.SetOrder.ReceivedRequest request)
        {
            statusCommandReceiver.SendSetOrderResponse(new BaseUnitStatus.SetOrder.Response(request.RequestId, new Empty()));

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Order = request.Payload.Order,
            });
        }
    }
}
