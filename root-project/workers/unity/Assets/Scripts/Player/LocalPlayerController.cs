using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class LocalPlayerController : NetworkPlayerController
    {
        [Require] BaseUnitStatusReader reader;

        protected override BaseUnitStatusReader Reader { get { return reader; } }

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
    }
}
