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
                ControllerUtils.GetSetKeyBits(KeyCode.Mouse0, ref bits);
                ControllerUtils.GetSetKeyBits(KeyCode.Mouse1, ref bits);
                ControllerUtils.GetSetKeyBits(KeyCode.F, ref bits);

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
