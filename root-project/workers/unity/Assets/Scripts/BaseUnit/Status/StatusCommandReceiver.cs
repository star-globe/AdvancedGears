using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class StatusCommandReceiver : MonoBehaviour
    {
        [Require] BaseUnitStatusWriter statusWriter;
        [Require] BaseUnitHealthWriter healthWriter;

        public void OnEnable()
        {
            statusWriter.OnForceStateEvent += OnForceState;
            statusWriter.OnSetOrderEvent += OnSetOrder;
        }

        private void OnSetOrder(OrderInfo info)
        {
            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Order = info.Order,
            });
        }

        private void OnForceState(ForceStateChange change)
        {
            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                Side = change.Side,
                State = change.State,
            });

            switch(change.State)
            {
                case UnitState.Alive:   Revive(); break;
                case UnitState.Sleep:   SetSleep(); break;
            }
        }

        private void Revive()
        {
            var health = healthWriter.Data;
            healthWriter.SendUpdate(new BaseUnitHealth.Update()
            {
                Health = health.MaxHealth,
                RecoveryAmount = 0,
            });
        }

        private void SetSleep()
        {
            var pos = this.transform.position;
            this.transform.position = new Vector3(pos.x, FixedParams.AbyssHeight, pos.z);
        }
    }
}
