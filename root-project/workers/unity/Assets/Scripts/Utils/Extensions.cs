using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.TransformSynchronization;

namespace AdvancedGears
{
    static class Extensions
    {
        public static Vector3 ToWorkerPosition(this FixedPointVector3 pos, Vector3 origin)
        {
            return pos.ToUnityVector() + origin;
        }

        public static FixedPointVector3 ToWorldPosition(this Vector3 pos, Vector3 origin)
        {
            return (pos - origin).ToFixedPointVector3();
        }

        public static float SqrMagnitude (this FixedPointVector3 vec)
        {
            return (vec.X * vec.X) + (vec.Y * vec.Y) + (vec.Z * vec.Z);
        }

        public static bool CheckTime(this ref IntervalChecker inter, float current, out float diff)
        {
            diff = inter.Buffer + inter.Interval;
            return CheckTime(ref inter, current);
        }

        public static bool CheckTime(this ref IntervalChecker inter, float current)
        {
            if (current - (inter.LastChecked + inter.Buffer) < inter.Interval)
                return false;

            inter.LastChecked = current;
            inter.Buffer = RandomInterval.GetRandom(inter.Interval);
            return true;
        }

        public static OrderPair Self(this OrderPair pair, OrderType self)
        {
            pair.Self = self;
            return pair;
        }

        public static OrderPair Upper(this OrderPair pair, OrderType upper)
        {
            pair.Upper = upper;
            return pair;
        }

        public static void SetData(this ref PostureInfo info, PostureData data)
        {
            if (info.Datas.ContainsKey(data.Point))
                info.Datas[data.Point] = data;
            else
                info.Datas.Add(data.Point, data);
        }

        public static void Resolve(this PostureTransform posture, Vector3 position, Transform effector, float angleSpeed = float.MaxValue)
        {
            if (posture.IsSet == false)
                return;

            var connectors = posture.Connectors;
            int length = connectors.Length;
            if (length == 0)
                return;

            Vector3 dmy = position;

            foreach (var c in connectors.Reverse())
                RotateAndMoveEffector(c, position, effector, angleSpeed);

            foreach (var c in connectors)
                RotateAndMoveEffector(c, position, effector, angleSpeed);
        }

        internal static Vector3 RotateAndMoveEffector(AttachedTransform attached, Vector3 tgt, Transform effector, float angleSpeed = float.MaxValue)
        {
            if (attached.HingeAxis == Vector3.zero)
                return Vector3.zero;

            var foward = (tgt - attached.transform.position).normalized;
            var vec = effector.position - attached.transform.position;
            var length = vec.magnitude;
            RotateLogic.Rotate(attached.transform, vec.normalized, attached.HingeAxis, foward, attached.Constrain, angleSpeed);

            return tgt - foward * length;
        }

        public static List<CompressedQuaternion> GetAllRotates(this UnitTransform unit, PosturePoint point)
        {
            if (unit.PostureDic.ContainsKey(point) == false)
                return new List<CompressedQuaternion>();

            return unit.PostureDic[point].Connectors.Select(c => c.transform.localRotation.ToCompressedQuaternion()).ToList();
        }

        public static T[] GetComponentsInChildrenWithoutSelf<T>(this GameObject self) where T : Component
        {
            return self.GetComponentsInChildren<T>().Where(c => self != c.gameObject).ToArray();
        }

        public static T GetComponentInChildrenWithoutSelf<T>(this GameObject self) where T : Component
        {
            return self.GetComponentsInChildrenWithoutSelf<T>().FirstOrDefault();
        }

        public static float GetAttackRange(this GunComponent.Component gun)
        {
            float length = 0;
            foreach (var kvp in gun.GunsDic)
            {
                var l = kvp.Value.AttackRange;
                if (length < l)
                    length = l;
            }

            return length;
        }

        public static void SetFollowers(this FollowerInfo info, List<EntityId> followers, List<EntityId> commanders)
        {
            foreach (var f in followers) {
                if (info.Followers.Exists(en => en.Id ==f.Id) == false)
                    info.Followers.Add(f);
            }

            foreach (var c in commanders)
            {
                if (info.UnderCommanders.Exists(en => en.Id == c.Id) == false)
                    info.UnderCommanders.Add(c);
            }
        }

        public static int EmptyCapacity(this FuelComponent.Component fuel)
        {
            return fuel.MaxFuel - fuel.Fuel;
        }

        public static float FuelRate(this FuelComponent.Component fuel)
        {
            return fuel.Fuel * 1.0f / fuel.MaxFuel;
        }

        public static bool Equals(this SupplyOrder self, SupplyOrder other)
        {
            return self.Type == other.Type &&
                   self.Point.StrongholdId == other.Point.StrongholdId;
        }

        public static UnitBaseType BaseType(this UnitType self)
        {
            switch(self)
            {
                case UnitType.Soldier:
                case UnitType.Commander:
                case UnitType.Advanced:
                case UnitType.Supply:
                case UnitType.Recon: 
                    return UnitBaseType.Moving;

                case UnitType.Stronghold:
                case UnitType.Turret:
                case UnitType.HeadQuarter:
                    return UnitBaseType.Fixed;
            }

            return UnitBaseType.None;
        }

        public static float[,] SetHeights(this TerrainPointInfo settings, Vector3 center, float x, float z, int width, float height, Vector3 size, float[,] b_heights)
        {
            float hillHeight = settings.HighestHillHeight - settings.LowestHillHeight;
            float[,] heights = new float[width, width];
            float sqr = (settings.Range * settings.Range);
            int seeds = settings.Seeds;
            float tileSize = settings.TileSize * 0.001f;

            for (int i = 0; i < width; i++) {
                for (int k = 0; k < width; k++) {
                    float pos_x = x + (i * 1.0f / width) * size.x;
                    float pos_z = z + (k * 1.0f / width) * size.z;
                    var diff = settings.LowestHillHeight + Mathf.PerlinNoise(pos_x * tileSize, pos_z * tileSize) * hillHeight;
                    var length = (pos_x - center.x) * (pos_x - center.x) + (pos_z - center.z) * (pos_z - center.z);
                    heights[i, k] = b_heights[i, k] + (diff / height) * Mathf.Exp(-length/sqr);
                }
            }

            Debug.LogFormat("height:{0}", heights[0,0]);

            Debug.LogFormat("0:{0}",Mathf.PerlinNoise(0, 10));
            Debug.LogFormat("-0.1f:{0}", Mathf.PerlinNoise(-0.1f, 10));

            return heights;
        }
    }

    public static class IntervalCheckerInitializer
    {
        public static IntervalChecker InitializedChecker(float inter)
        {
            return new IntervalChecker(inter,0,0,-1);
        }
    }
}

