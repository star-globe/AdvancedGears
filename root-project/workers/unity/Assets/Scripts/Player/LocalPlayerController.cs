using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class LocalPlayerController : BasePlayerController
    {
        [Require] BaseUnitStatusReader reader;

        protected override long TriggerBits
        {
            get
            {
                long bits = 0;
                if (Input.GetKey(KeyCode.Mouse0))
                    bits = ControllerUtils.GetBits(KeyCode.Mouse0);

                return bits;
            }
        }

        protected override UnitSide SelfSide
        {
            get
            {
                if (reader == null)
                    return UnitSide.None;

                return reader.Data.Side;
            }
        }
    }
}
