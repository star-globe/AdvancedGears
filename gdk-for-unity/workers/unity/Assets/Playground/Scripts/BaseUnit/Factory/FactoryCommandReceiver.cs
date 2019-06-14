using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class FactoryCommandReceiver : MonoBehaviour
    {
        [Require] UnitFactoryCommandReceiver factoryCommandReceiver;
        [Require] UnitFactoryWriter factoryWriter;

        public void OnEnable()
        {
            factoryCommandReceiver.OnAddOrderRequestReceived += OnAddOrderRequest;
        }

        private void OnAddOrderRequest(UnitFactory.AddOrder.ReceivedRequest request)
        {
            factoryCommandReceiver.SendAddOrderResponse(new UnitFactory.AddOrder.Response(request.RequestId, new Empty()));

            var list = factoryWriter.Data.Orders;
            list.Add(request.Payload);
            factoryWriter.SendUpdate(new UnitFactory.Update()
            {
                Orders = list,
            });
        }
    }
}
