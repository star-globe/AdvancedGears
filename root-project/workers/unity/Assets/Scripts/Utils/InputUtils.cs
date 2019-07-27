using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public static class InputUtils
    {
        private static bool IsMove
        {
            get { return Input.GetKey(KeyCode.C) == false; }
        }

        public static Vector3 GetMove(in Vector3 right, in Vector3 forward)
        {
            if (IsMove)
                return Input.GetAxisRaw("Horizontal") * right + Input.GetAxisRaw("Vertical") * forward;
            else
                return Vector3.zero;
        }

        public static float CameraX
        {
            get { return IsMove ? 0.0f: Input.GetAxis("Horizontal"); }
        }

        public static float CameraY
        {
            get { return IsMove ? 0.0f: Input.GetAxis("Vertical"); }
        }
    }

}

