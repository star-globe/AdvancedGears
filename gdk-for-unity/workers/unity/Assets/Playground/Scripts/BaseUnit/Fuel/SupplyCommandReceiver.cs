using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Improbable.Common;

namespace Playground
{
    public class SupplyCommandReceiver : MonoBehaviour
    {
        [Require] FuelSupplyerCommandReceiver commandReceiver;
        [Require] FuelSupplyerWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnSetOrderRequestReceived += OnSetOrderRequest;
        }

        private void OnSetOrderRequest(FuelSupplyer.SetOrder.ReceivedRequest request)
        {
            commandReceiver.SendSetOrderResponse(new FuelSupplyer.SetOrder.Response(request.RequestId, new Empty()));

            writer.SendUpdate(new FuelSupplyer.Update()
            {
                Order = request.Payload,
                OrderFinished = new BlittableBool(false),
            });
        }
    }
}
