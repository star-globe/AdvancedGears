using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Improbable.Gdk.Subscriptions;

namespace AdvancedGears
{
    public class OtherPlayerController : BasePlayerController
    {
        [Require]
        AdvancedUnitControllerReader reader;

        protected override bool IsTrigger
        {
            get
            {
                if (reader == null)
                    return false;

                return reader.Data.Action.LeftClick;
            }
        }
    }
}
