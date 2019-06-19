using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class SupplyManagerCommandReceiver : MonoBehaviour
    {
        [Require] FuelSupplyManagerCommandReceiver commandReceiver;
        [Require] FuelSupplyManagerWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnFinishOrderRequestReceived += OnFinishOrderRequest;
        }

        private void OnFinishOrderRequest(FuelSupplyManager.FinishOrder.ReceivedRequest request)
        {
            var newOrder = new SupplyOrder { Type = SupplyOrderType.None };

            var manager = writer.Data;

            var order = request.Payload;
            // TODO:result check
            if (manager.SupplyOrders.ContainsKey(order.SelfId))
            {
                var plan = manager.SupplyOrders[order.SelfId];
                plan.Orders.RemoveAll(o => o.Equals(order));

                if (plan.Orders.Count > 0) {
                    newOrder.Type = plan.Orders[0].Type;
                }
                else {
                    manager.SupplyOrders.Remove(order.SelfId);
                    manager.FreeSupplyers.Add(order.SelfId);
                }
            }

            commandReceiver.SendFinishOrderResponse(new FuelSupplyManager.FinishOrder.Response(request.RequestId, newOrder));

            writer.SendUpdate(new FuelSupplyManager.Update()
            {
                SupplyOrders = manager.SupplyOrders,
                FreeSupplyers = manager.FreeSupplyers,
            });
        }
    }
}
