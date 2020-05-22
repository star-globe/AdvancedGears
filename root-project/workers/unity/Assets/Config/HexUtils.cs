using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.TransformSynchronization;
using UnityEngine;

namespace AdvancedGears
{
    public static class HexUtils
    {
        const float edgeLength = 500.0f;
        readonly static float route3 = Mathf.Sqrt(3.0f);

        public static void SetHexCorners(in Vector3 center, Vector3[] corners, float edge = edgeLength)
        {
            if (corners == null || corners.Length != 6)
                return;

            corners[0] = center + new Vector3(edge * route3 / 2, 0, edge * 0.5f);
            corners[1] = center + new Vector3(0, 0, edge);
            corners[2] = center + new Vector3(-edge * route3 / 2, 0, edge * 0.5f);
            corners[3] = center + new Vector3(-edge * route3 / 2, 0, -edge * 0.5f);
            corners[4] = center + new Vector3(0, 0, -edge);
            corners[5] = center + new Vector3(edge * route3 / 2, 0, -edge * 0.5f);
        }

        public static void SetHexCorners(in Vector3 origin, uint index, Vector3[] corners, float edge = edgeLength)
        {
            SetHexCorners(GetHexCenter(origin, index, edge), corners, edge);
        }

        public static Vector3 GetHexCenter(in Vector3 origin, uint index, float edge = edgeLength)
        {
            if (index == 0)
                return origin;

            Vector3 pos = origin;

            var i = index;
            uint n = 0;
            uint direct = 0;
            uint rest = 0;
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

        public static uint[] GetNeighborHexIndexes(uint index)
        {
            var ids = new uint[6] { 1, 2, 3, 4, 5, 6};

            var i = index;
            uint n = 0;
            uint direct = 0;
            uint rest = 0;
            while(i > 0) {
                n++;
                direct = (i-1)/n;
                rest = (i-1)%n;
                i -= 6 * n;
            }

            //int baseNum = 0;
            switch(direct)
            {
                case 0: ids[0] = index + 6*n;
                        ids[1] = ids[0] + 1;
                        ids[2] = index + 1;
                        ids[3] = index == 1 ? 0: index - 6*(n-1);
                        ids[4] = rest == 0 ? 6*n*(n+1)/2: ids[3] - 1;
                        ids[5] = rest == 0 ? 6*(n+1)*(n+2)/2: index - 1;
                        break;
                case 1: 
                case 2: 
                case 3: 
                case 4:
                case 5:
                        break;
            }

            return ids;
        }
    }
}
