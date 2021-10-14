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

        protected abstract BaseUnitStatusReader Reader { get; }

        protected override UnitSide SelfSide
        {
            get
            {
                if (this.Reader == null)
                    return UnitSide.None;

                return this.Reader.Data.Side;
            }
        }

        protected override bool IsDead
        {
            get
            {
                if (this.Reader == null)
                    return true;

                return this.Reader.Data.State == UnitState.Dead;
            }
        }
    }
}
