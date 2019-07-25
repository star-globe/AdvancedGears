using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class FactoryCommandReceiver : MonoBehaviour
    {
        [Require] UnitFactoryCommandReceiver commandReceiver;
        [Require] UnitFactoryWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnAddFollowerOrderRequestReceived += OnAddFollowerOrderRequest;
            commandReceiver.OnAddSuperiorOrderRequestReceived += OnAddSuperiorOrderRequest;
        }

        private void OnAddFollowerOrderRequest(UnitFactory.AddFollowerOrder.ReceivedRequest request)
        {
            commandReceiver.SendAddFollowerOrderResponse(new UnitFactory.AddFollowerOrder.Response(request.RequestId, new Empty()));

            var list = writer.Data.FollowerOrders;
            list.Add(request.Payload);
            writer.SendUpdate(new UnitFactory.Update()
            {
                FollowerOrders = list,
            });
        }

        private void OnAddSuperiorOrderRequest(UnitFactory.AddSuperiorOrder.ReceivedRequest request)
        {
            commandReceiver.SendAddSuperiorOrderResponse(new UnitFactory.AddSuperiorOrder.Response(request.RequestId, new Empty()));

            var list = writer.Data.SuperiorOrders;
            list.Add(request.Payload);
            writer.SendUpdate(new UnitFactory.Update()
            {
                SuperiorOrders = list,
            });
        }
    }
}
