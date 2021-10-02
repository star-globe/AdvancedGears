using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Improbable;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.Core;

namespace AdvancedGears
{
    public class ControllerUtils
    {
        public static long GetBits(KeyCode code)
        {
            long bits = 0;
            return GetBits(code, bits);
        }

        public static long GetBits(KeyCode code, long bits)
        {
            switch (code)
            {
                case KeyCode.Mouse0:
                    bits |= 1 << 0;
                    break;

                case KeyCode.Mouse1:
                    bits |= 1 << 1;
                    break;
            }

            return bits;
        }
    }
}
