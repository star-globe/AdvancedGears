using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class SupplyCommandReceiver : MonoBehaviour
    {
        [Require] FuelSupplyerWriter writer;

        public void OnEnable()
        {
            writer.OnSetOrderEvent += OnSetOrder;
        }

        private void OnSetOrder(SupplyOrder order)
        {
            writer.SendUpdate(new FuelSupplyer.Update()
            {
                Order = order,
                OrderFinished = false,
            });
        }
    }
}
