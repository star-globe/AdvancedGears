using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class SleepEventReceiver : MonoBehaviour
    {
        [Require] SleepComponentWriter sleepWriter;
        [Require] BaseUnitStatusWriter statusWriter;

        public void OnEnable()
        {
            statusWriter.OnForceStateEvent += OnForceState;
            sleepWriter.OnSleepOrderedEvent += OnSleepOrdered;
        }

        private void OnForceState(ForceStateChange change)
        {
            if (change.State != UnitState.Sleep)
                return;

            var inter = sleepWriter.Data.Interval;
            inter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.SleepInter);
            sleepWriter.SendUpdate(new SleepComponent.Update()
            {
                Interval = inter,
            });
        }

        private void OnSleepOrdered(SleepOrderInfo info)
        {
            if (statusWriter.Data.State == UnitState.Dead)
                return;

            UnitState state = UnitState.None;
            switch(info.Order)
            {
                case SleepOrderType.Sleep:  state = UnitState.Sleep; break;
                case SleepOrderType.WakeUp: state = UnitState.Alive; return;
            }

            if (state == UnitState.None)
                return;

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                State = state,
            });
        }
    }
}
