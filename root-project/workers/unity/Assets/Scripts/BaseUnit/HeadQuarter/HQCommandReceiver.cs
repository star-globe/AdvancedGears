using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class HQCommandReceiver : MonoBehaviour
    {
        [Require] HeadQuartersCommandReceiver commandReceiver;
        [Require] HeadQuartersWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnAddOrderRequestReceived += OnAddFollowerOrderRequest;
        }

        private void OnAddFollowerOrderRequest(HeadQuarters.AddOrder.ReceivedRequest request)
        {
            commandReceiver.SendAddOrderResponse(new HeadQuarters.AddOrder.Response(request.RequestId, new Empty()));

            var list = writer.Data.Orders;
            list.Add(request.Payload);
            writer.SendUpdate(new HeadQuarters.Update()
            {
                Orders = list,
            });
        }
    }
}
