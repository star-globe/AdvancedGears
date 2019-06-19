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

        void OnModified(FuelModifier modifier)
        {
            var current = writer.Data.Fuel;
            var max = writer.Data.MaxFuel;
            switch (modifier.Type)
            {
                case FuelModifyType.Consume:
                case FuelModifyType.Absorb:     current -= modifier.Amount;  break;
                case FuelModifyType.Feed:       current += modifier.Amount;  break;
            }

            current = Mathf.Clamp(current,0,max);

            writer.SendUpdate(new FuelComponent.Update()
            {
                Fuel = current,
            });
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
                    newOrder.Type = plan.Orders[0];
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
