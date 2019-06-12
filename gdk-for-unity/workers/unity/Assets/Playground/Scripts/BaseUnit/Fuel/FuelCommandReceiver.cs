using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Common;

namespace Playground
{
    public class FuelCommandReceiver : MonoBehaviour
    {
        [Require] FuelComponentCommandReceiver commandReceiver;
        [Require] FuelComponentWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnModifyFuealRequestReceived += OnAddOrderRequest;
            writer.OnFuelModifiedEvent += OnModifed;
        }

        void OnModifed(FuelModifier modifier)
        {
            var current = writer.Data.Fuel;
            var max = writer.Data.MaxFuel;
            switch (modifier.Type)
            {
                case FuelModifyType.Consume:    current - modifier.Amount;  break;
                case FuelModifyType.Feed:       current + modifier.Amount;  break;
            }

            current = Mathf.Clamp(current,0,max);

            writer.SendUpdate(new FuelComponent.Update()
            {
                FuelCommandReceiver = current,
            });
        }

        private void OnAddOrderRequest(FuelComponent.ModifyFueal.ReceivedRequest request)
        {
            commandReceiver.SendModifyFuelResponse(new FuelComponent.ModifyFueal.Response(request.RequestId, new Empty()));

            writer.Data.Orders;
            list.Add(request.Payload);
            writer.SendUpdate(new FuelComponent.Update()
            {
                orders = list,
            });
        }
    }
}
