using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class FuelCommandReceiver : MonoBehaviour
    {
        [Require] FuelComponentCommandReceiver commandReceiver;
        [Require] FuelComponentWriter writer;

        public void OnEnable()
        {
            commandReceiver.OnModifyFuelRequestReceived += OnAddOrderRequest;
            writer.OnFuelModifiedEvent += OnModified;
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

        private void OnAddOrderRequest(FuelComponent.ModifyFuel.ReceivedRequest request)
        {
            commandReceiver.SendModifyFuelResponse(new FuelComponent.ModifyFuel.Response(request.RequestId, new Empty()));

            OnModified(request.Payload);
        }
    }
}
