using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Entities;
using Improbable.Gdk;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class CommanderUnitInitializer : MonoBehaviour
    {
        [Require] CommanderSightWriter sight;
        [Require] CommanderStatusWriter commander;
        [Require] CommanderActionWriter action;
        [Require] BoidComponentWriter boid;
        [Require] BaseUnitStatusReader status;
        [Require] DominationDeviceWriter domination;
        [Require] World world;

        [SerializeField]
        CommanderUnitInitSettings settings;

        void Start()
        {
            sight.SendUpdate(new CommanderSight.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
                Range = settings.SightRange,
            });

            commander.SendUpdate(new CommanderStatus.Update
            {
                AllyRange = settings.AllyRange,
                //TeamConfig = settings.TeamConfig,
            });

            action.SendUpdate(new CommanderAction.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
            });

            boid.SendUpdate(new BoidComponent.Update
            {
                ForwardLength = settings.ForwardLength,
                SepareteWeight = settings.SepareteWeight,
                AlignmentWeight = settings.AlignmentWeight,
                CohesionWeight = settings.CohesionWeight,
            });

            domination.SendUpdate(new DominationDevice.Update
            {
                Speed = settings.CaptureSpeed,
            });
        }
    }
}
