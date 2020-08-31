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
    public class StrongholdUnitInitializer : MonoBehaviour
    {
        [Require] StrongholdSightWriter sight;
        [Require] DominationStaminaWriter stamina;

        [SerializeField]
        StrongholdUnitInitSettings settings;

        void Start()
        {
            sight.SendUpdate(new StrongholdSight.Update
            {
                Interval = IntervalCheckerInitializer.InitializedChecker(settings.Inter),
            });

            stamina.SendUpdate(new DominationStamina.Update
            {
                Range = settings.DominationRange,
                MaxStamina = settings.MaxStamina,
            });
        }
    }
}
