using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public abstract class NetworkPlayerController : BasePlayerController
    {
        [Require] BaseUnitStatusReader reader;

        protected override UnitSide SelfSide
        {
            get
            {
                if (reader == null)
                    return UnitSide.None;

                return reader.Data.Side;
            }
        }

        protected override bool IsDead
        {
            get
            {
                if (reader == null)
                    return true;

                return reader.Data.State == UnitState.Dead;
            }
        }
    }
}
