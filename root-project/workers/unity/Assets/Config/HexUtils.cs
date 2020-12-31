using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedGears
{
    public static class HexUtils
    {
        const float edgeLength = 500.0f;
        readonly static float route3 = Mathf.Sqrt(3.0f);
        const float powerMinLimit = 1.0f / 1000;

        public static void SetHexCorners(in Vector3 center, Vector3[] corners, float edge = edgeLength)
        {
            if (corners == null || corners.Length != 7)
                return;

            corners[0] = center + new Vector3(edge * route3 / 2, 0, edge * 0.5f);
            corners[1] = center + new Vector3(0, 0, edge);
            corners[2] = center + new Vector3(-edge * route3 / 2, 0, edge * 0.5f);
            corners[3] = center + new Vector3(-edge * route3 / 2, 0, -edge * 0.5f);
            corners[4] = center + new Vector3(0, 0, -edge);
            corners[5] = center + new Vector3(edge * route3 / 2, 0, -edge * 0.5f);
            corners[6] = corners[0];
        }

        public static void SetHexCorners(in Vector3 origin, uint index, Vector3[] corners, float edge = edgeLength)
        {
            SetHexCorners(GetHexCenter(origin, index, edge), corners, edge);
        }

        public static bool IsInsideHex(in Vector3 origin, uint index, in Vector3 pos, float edge = edgeLength)
        {
            var corners = new Vector3[7];
            SetHexCorners(origin, index, corners, edge);

            for(var i = 0; i < 6; i++) {
                var from = corners[i];
                var to = corners[i+1];

                var cross = Vector3.Cross(pos-from, to-from);
                if (Vector3.Dot(cross, Vector3.up) < 0)
                    return false;
            }

            return true;
        }

        private static void CalcIndex(uint index, out uint n, out uint direct, out uint rest)
        {
            var i = index;
            n = 0;
            direct = 0;
            rest = 0;
            while (i > 0) {
                n++;
                direct = (i - 1) / n;
                rest = (i - 1) % n;

                if (i > 6 * n)
                    i -= 6 * n;
                else
                    break;
            }
        }

        public static bool CheckLine(Vector3 left, Vector3 right, Vector3[] corners, float checkLength)
        {
            if (corners.Length != 7)
                return false;

            for (var i = 0; i < corners.Length - 1; i++) {
                if ((corners[i] - right).sqrMagnitude >= checkLength)
                    continue;

                if ((corners[i+1] - left).sqrMagnitude >= checkLength)
                    continue;

                return true;
            }

            return false;
        }

        public static Vector3 GetHexCenter(in Vector3 origin, uint index, float edge = edgeLength)
        {
            if (index == 0)
                return origin;

            Vector3 pos = origin;

            CalcIndex(index, out var n, out var direct, out var rest);

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
                        roll = new Vector3(edge * route3/2, 0, -edge * 1.5f); break;
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

            if (index == 0)
                return ids;

            CalcIndex(index, out var n, out var direct, out var rest);

            switch (direct)
            {
                case 0: ids[0] = index + 6*n;
                        ids[1] = index + 6*n+1;
                        ids[2] = index + 1;
                        ids[3] = index == 1 ? 0: index - 6*(n-1);
                        ids[4] = rest == 0 ? index + 6*n-1: index - (6*(n-1)+1);
                        ids[5] = rest == 0 ? index + 6*(2*n+1)-1: index - 1;
                        break;

                case 1: ids[0] = rest == 0 ? index + 6*n: index - 1;
                        ids[1] = index + 6*n+1;
                        ids[2] = index + 6*n+2;
                        ids[3] = index + 1;
                        ids[4] = index == 2 ? 0: index - (6*(n-1)+1);
                        ids[5] = rest == 0 ? index - 1: index - (6*(n-1)+2);
                        break;

                case 2: ids[0] = rest == 0 ? index - 1: index - (6*(n-1)+3);
                        ids[1] = rest == 0 ? index + 6*n+1: index - 1;
                        ids[2] = index + 6*n+2;
                        ids[3] = index + 6*n+3;
                        ids[4] = index + 1;
                        ids[5] = index == 3 ? 0: index - (6*(n-1)+2);
                        break;

                case 3: ids[0] = index == 4 ? 0: index - (6*(n-1)+3);
                        ids[1] = rest == 0 ? index - 1: index - (6*(n-1)+4);
                        ids[2] = rest == 0 ? index + 6*n+2: index - 1;
                        ids[3] = index + 6*n+3;
                        ids[4] = index + 6*n+4;
                        ids[5] = index + 1;
                        break;

                case 4: ids[0] = index + 1;
                        ids[1] = index == 5 ? 0: index - (6*(n-1)+4);
                        ids[2] = rest == 0 ? index - 1: index - (6*(n-1)+5);
                        ids[3] = rest == 0 ? index + (6*n+3): index - 1;
                        ids[4] = index + 6*n+4;
                        ids[5] = index + 6*n+5; 
                        break;

                case 5: ids[0] = index + 6*(n+1);
                        ids[1] = index % 6 == 0 ? index - (6*n-1): index + 1;
                        ids[2] = index == 6 ? 0: (index % 6 == 0 ? index - (6*(2*n-1)-1): index - (6*(n-1)+5));
                        ids[3] = rest == 0 ? index - 1: index - 6*n;
                        ids[4] = rest == 0 ? index + 6*n+4: index - 1;
                        ids[5] = index + 6*n+5;
                        break;
            }

            return ids;

            // another calculate method
            for(uint i = 0; i <= 5; i++) {
                uint ind = (direct + i) % 6;
                uint num = 0;
                switch(i)
                {
                    case 0:
                    case 1:
                        num = index + 6*n + direct + i;
                        break;
            
                    case 2:
                        if (direct == 5 && rest == n - 1)
                            num = index - (6*n-1);
                        else
                            num = index + 1;
                        break;
            
                    case 3:
                        num = index == direct+1 ? 0: index - (6*n + direct - 6);
                        break;
            
                    case 4:
                        num = rest == 0 ? index - 1: index - (6*n + direct - 5);
                        break;
                    case 5:
                        if (rest > 0)
                            num = index - 1;
                        else
                            num = direct > 0 ? index + 6*n + direct -1:
                                               index + 6*n + 5;
                        break;
                }
            
                ids[ind] = num;
            }

            return ids;
        }

        public static UnitType GetUnitType(HexAttribute attribute)
        {
            var type = UnitType.None;
            switch (attribute)
            {
                case HexAttribute.Field:
                case HexAttribute.ForwardBase:
                    type = UnitType.Stronghold;
                    break;

                case HexAttribute.CentralBase:
                    type = UnitType.HeadQuarter;
                    break;
            }

            return type;
        }

        public static IEnumerable<UnitSide> AllSides
        {
            get
            {
                var side = UnitSide.None;
                while (side < UnitSide.Num) {
                    yield return side;
                    side++;
                }
            }
        }

        public static bool ExistOtherSidePowers(HexIndex hex, UnitSide self)
        {
            if (hex.SidePowers == null)
                return false;

            foreach (var kvp in hex.SidePowers)
            {
                if (kvp.Key != self && kvp.Value > powerMinLimit)
                    return true;
            }

            return false;
        }

        public static bool TryGetOneSidePower(HexIndex hex, out UnitSide side, out float val)
        {
            side = UnitSide.None;
            val = 0;

            if (hex.SidePowers == null)
                return false;

            var keys = hex.SidePowers.Where(kvp => kvp.Value > powerMinLimit).Select(kvp => kvp.Key).ToArray();
            if (keys.Length == 1) {
                side = keys[0];
                val = hex.SidePowers[side];
                return true;
            }

            return false;
        }
    }
}
