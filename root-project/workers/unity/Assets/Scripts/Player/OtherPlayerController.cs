using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class OtherPlayerController : BasePlayerController
    {
        [Require] BaseUnitStatusReader statusReader;
        [Require] AdvancedUnitControllerReader reader;

        protected override long TriggerBits
        {
            get
            {
                if (reader == null)
                    return 0;

                return reader.Data.Action.ClickBits;
            }
        }

        protected override UnitSide SelfSide
        {
            get
            {
                if (statusReader == null)
                    return UnitSide.None;

                return statusReader.Data.Side;
            }
        }
    }
}
