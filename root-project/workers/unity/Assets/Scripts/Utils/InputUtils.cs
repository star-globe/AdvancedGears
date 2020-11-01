using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    public static class InputUtils
    {
        public static Vector3 GetMove(in Vector3 right, in Vector3 forward)
        {
            return Input.GetAxisRaw("Horizontal") * right + Input.GetAxisRaw("Vertical") * forward;
        }

        public static Vector2 GetMove()
        {
            return Input.GetAxisRaw("Horizontal") * Vector2.right + Input.GetAxisRaw("Vertical") * Vector2.up;
        }

        public static Vector2 GetCamera()
        {
            return Input.GetAxis("Mouse X")*Vector2.right + Input.GetAxis("Mouse Y")*Vector2.up;
        }

        public static float CameraX
        {
            get { return Input.GetAxis("Mouse X"); }
        }

        public static float CameraY
        {
            get { return Input.GetAxis("Mouse Y"); }
        }
    }
}

