using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using Unity.Entities;
using System.Collections.Generic;
using AdvancedGears.UI;

namespace AdvancedGears
{
    public static class HexUtils
    {
        const float edgeLength = 500.0f;
        readonly static float route3 = Mathf.Sqrt(3.0f);
        public static void SetHexCorners(in Vector3 origin, int index, Vector3[] corners, float edge = edgeLength)
        {
            if (corners == null || corners.Length != 6)
                return;

            var center = GetHexCenter(origin, index, edge);
            corners[0] = center + new Vector3(edge * route3/2, 0, edge * 0.5f);
            corners[1] = center + new Vector3(0, 0, edge);
            corners[2] = center + new Vector3(-edge * route3/2, 0, edge * 0.5f);
            corners[3] = center + new Vector3(-edge * route3/2, 0, -edge * 0.5f);
            corners[4] = center + new Vector3(0, 0, -edge);
            corners[5] = center + new Vector3(edge * route3/2, 0, -edge * 0.5f);
        }

        public static Vector3 GetHexCenter(in Vector3 origin, int index, float edge = edgeLength)
        {
            if (index == 0)
                return origin;

            Vector3 pos = origin;

            var i = index;
            int n = 0;
            int direct = 0;
            int rest = 0;
            while(i > 0) {
                n++;
                direct = (i-1)/n;
                rest = (i-1)%n;
                i -= 6 * n;
            }

            Vector3 spread = Vector3.zero;
            Vector3 roll = Vector3.zero;
            switch(direct)
            {
                case 0: spread = new Vector3(edge * route3, 0, 0);
                        roll = new Vector3(-edge * route3/2, 0, edge * 1.5f); break;
                case 1: spread = new Vector3(edge * route3/2, 0, edge * 1.5f);
                        roll = new Vector3(-edge * route3, 0, 0); break;
                case 2: spread = new Vector3(-edge * route3/2, 0, edge * 1.5f);
                        roll = new Vector3(-edge * route3/2, 0, -edge * 1.5f); break;
                case 3: spread = new Vector3(-edge * route3, 0, 0);
                        roll = new Vector3(edge * route3, 0, -edge * 1.5f); break;
                case 4: spread = new Vector3(-edge * route3/2, 0, -edge * 1.5f);
                        roll = new Vector3(edge * route3, 0, 0); break;
                case 5: spread = new Vector3(edge * route3/2, 0, -edge * 1.5f);
                        roll = new Vector3(edge * route3/2, 0, edge * 1.5f); break;
            }

            pos += spread * n + roll * rest;
            return pos;
        }
    }
}
