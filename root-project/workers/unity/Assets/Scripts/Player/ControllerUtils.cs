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
        public static void GetSetKeyBits(KeyCode code, ref long bits)
        {
            if (Input.GetKey(code))
                bits = GetBits(code, bits);
        }

        public static long GetBits(KeyCode code)
        {
            long bits = 0;
            return GetBits(code, bits);
        }

        public static long GetBits(KeyCode code, long bits)
        {
            switch (code)
            {
                case KeyCode.Mouse0: bits |= 1 << 0; break;
                case KeyCode.Mouse1: bits |= 1 << 1; break;
                case KeyCode.Mouse2: bits |= 1 << 2; break;
                case KeyCode.Mouse3: bits |= 1 << 3; break;
                case KeyCode.Mouse4: bits |= 1 << 4; break;
                case KeyCode.Mouse5: bits |= 1 << 5; break;
                case KeyCode.Mouse6: bits |= 1 << 6; break;

                case KeyCode.A: bits |= 1 << 16; break;
                case KeyCode.B: bits |= 1 << 17; break;
                case KeyCode.C: bits |= 1 << 18; break;
                case KeyCode.D: bits |= 1 << 19; break;
                case KeyCode.E: bits |= 1 << 20; break;
                case KeyCode.F: bits |= 1 << 21; break;
                case KeyCode.G: bits |= 1 << 22; break;
                case KeyCode.H: bits |= 1 << 23; break;
                case KeyCode.I: bits |= 1 << 24; break;
                case KeyCode.J: bits |= 1 << 25; break;
                case KeyCode.K: bits |= 1 << 26; break;
                case KeyCode.L: bits |= 1 << 27; break;
                case KeyCode.M: bits |= 1 << 28; break;
                case KeyCode.N: bits |= 1 << 29; break;
                case KeyCode.O: bits |= 1 << 30; break;
                case KeyCode.P: bits |= 1 << 31; break;
                case KeyCode.Q: bits |= 1 << 32; break;
                case KeyCode.R: bits |= 1 << 33; break;
                case KeyCode.S: bits |= 1 << 34; break;
                case KeyCode.T: bits |= 1 << 35; break;
                case KeyCode.U: bits |= 1 << 36; break;
                case KeyCode.V: bits |= 1 << 37; break;
                case KeyCode.W: bits |= 1 << 38; break;
                case KeyCode.X: bits |= 1 << 39; break;
                case KeyCode.Y: bits |= 1 << 40; break;
                case KeyCode.Z: bits |= 1 << 41; break;
            }

            return bits;
        }
    }
}
