using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;
using Unity.Entities;

namespace AdvancedGears
{
    public class SleepEventReceiver : MonoBehaviour
    {
        [Require] World world;
        [Require] SleepComponentWriter sleepWriter;
        [Require] BaseUnitStatusWriter statusWriter;

        SpatialComponentSystem system = null;

        public void OnEnable()
        {
            statusWriter.OnForceStateEvent += OnForceState;
            sleepWriter.OnSleepOrderedEvent += OnSleepOrdered;

            system = world.GetExistingSystem<SleepUpdateSystem>();
        }

        private void OnForceState(ForceStateChange change)
        {
            if (change.State != UnitState.Alive)
                return;

            UpdateSleepInter();
        }

        private void OnSleepOrdered(SleepOrderInfo info)
        {
            if (statusWriter.Data.State == UnitState.Dead)
                return;

            UnitState state = UnitState.None;
            switch(info.Order)
            {
                case SleepOrderType.Sleep:  state = UnitState.Sleep; break;
                case SleepOrderType.WakeUp: state = UnitState.Alive; break;
            }

            if (state == UnitState.None)
                return;

            statusWriter.SendUpdate(new BaseUnitStatus.Update()
            {
                State = state,
            });

            UpdateSleepInter();
        }

        private void UpdateSleepInter()
        {
            if (system == null)
                return;

            var inter = sleepWriter.Data.Interval;
            if (inter.Interval != MovementDictionary.SleepInter)
                inter = IntervalCheckerInitializer.InitializedChecker(MovementDictionary.SleepInter);

            inter.UpdateLastChecked(system.Time.ElapsedTime);
            sleepWriter.SendUpdate(new SleepComponent.Update()
            {
                Interval = inter,
            });
        }
    }
}
