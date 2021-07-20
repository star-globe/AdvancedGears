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
        public static double SqrMagnitude(this Coordinates coords)
        {
            return (coords.X * coords.X) + (coords.Y * coords.Y) + (coords.Z * coords.Z);
        }
        
        public static Vector3 ToWorkerPosition(this Coordinates coords, Vector3 origin)
        {
            return coords.ToUnityVector() + origin;
        }

        public static Vector3 ToWorkerPosition(this FixedPointVector3 pos, Vector3 origin)
        {
            return pos.ToUnityVector() + origin;
        }

        public static FixedPointVector3 ToWorldPosition(this Vector3 pos, Vector3 origin)
        {
            return (pos - origin).ToFixedPointVector3();
        }

        public static Coordinates ToWorldCoordinates(this Vector3 pos, Vector3 origin)
        {
            return (pos - origin).ToCoordinates();
        }

        public static Coordinates ToCoordinates(this Vector3 pos)
        {
            return new Coordinates(pos.x, pos.y, pos.z);
        }

        public static Coordinates ToCoordinates(this FixedPointVector3 pos)
        {
            return pos.ToUnityVector().ToCoordinates();
        }

        public static FixedPointVector3 ToFixedPointVector3(this Coordinates coords)
        {
            return coords.ToUnityVector().ToFixedPointVector3();
        }

        public static float SqrMagnitude (this FixedPointVector3 vec)
        {
            return (vec.X * vec.X) + (vec.Y * vec.Y) + (vec.Z * vec.Z);
        }

        public static void SetUpwards(this Transform transform, Vector3 up)
        {
            var f = transform.forward;
            transform.rotation = Quaternion.LookRotation(f, up);
        }

        public static bool CheckTime(this ref IntervalChecker inter, double current)
        {
            if (current - (inter.LastChecked + inter.Buffer) < inter.Interval)
                return false;

            inter.LastChecked = current;
            inter.Buffer = RandomInterval.GetRandom(inter.Interval);
            return true;
        }

        public static void UpdateLastChecked(this ref IntervalChecker inter, double current)
        {
            inter.LastChecked = current;
        }

        public static bool Self(this OrderPair pair, OrderType self)
        {
            bool changed = pair.Self != self;
            pair.Self = self;
            return changed;
        }

        public static bool Upper(this OrderPair pair, OrderType upper)
        {
            bool changed = pair.Upper != upper;
            pair.Upper = upper;
            return changed;
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
                var l = kvp.Value.AttackRange();
                if (length < l)
                    length = l;
            }

            return length;
        }

        public static void SetFollowers(this FollowerInfo info, List<EntityId> followers, List<EntityId> commanders)
        {
            foreach (var f in followers) {
                if (info.Followers.Any(en => en.Id ==f.Id) == false)
                    info.Followers.Add(f);
            }

            foreach (var c in commanders)
            {
                if (info.UnderCommanders.Any(en => en.Id == c.Id) == false)
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

        public static void SetHeights(this TerrainPointInfo settings, Vector3 center, float x, float z, int width, float height, Vector3 size, float[,] b_heights)
        {
            float hillHeight = settings.HighestHillHeight - settings.LowestHillHeight;
            float sqr = (settings.Range * settings.Range);
            int seeds = settings.Seeds;
            float tileSize = settings.TileSize * 0.001f;

            float base_x = size.x / width;
            float base_z = size.z / width;

            for (int i = 0; i < width; i++) {
                for (int k = 0; k < width; k++) {

                    float pos_x = x + k * size.x / width;
                    float noise_x = (x + (k+seeds) * base_x) / size.x;

                    float pos_z = z + i * base_z;
                    float noise_z = (z + (i+seeds) * base_z) / size.z;

                    var diff = settings.LowestHillHeight + Mathf.PerlinNoise(noise_x * tileSize, noise_z * tileSize) * hillHeight;
                    var length = (pos_x - center.x) * (pos_x - center.x) + (pos_z - center.z) * (pos_z - center.z);
                    b_heights[i, k] = b_heights[i, k] + (diff / height) * Mathf.Exp(-length/sqr);
                }
            }
        }

        public static List<EntityId> GetAllFollowers(this FollowerInfo info, List<EntityId> list)
        {
            if (list != null) {
                list.Clear();
                list.AddRange(info.Followers);
                list.AddRange(info.UnderCommanders);
            }

            return list;
        }

        /// <summary>
        /// RigidBodyの停止
        /// </summary>
        /// <param name="rigidbody"></param>
        public static void Stop(this Rigidbody rigidbody, bool isRotating = false)
        {
            rigidbody.velocity = Vector3.zero;
            if (isRotating == false)
                rigidbody.angularVelocity = Vector3.zero;
        }

        //----CheckPoor----
        public static bool IsPoor(this BaseUnitHealth.Component health)
        {
            return health.Health < health.MaxHealth;
        }

        public static bool IsPoor(this FuelComponent.Component fuel)
        {
            return fuel.Fuel < fuel.MaxFuel;
        }

        public static bool IsPoor(this GunInfo gun)
        {
            return gun.StockBullets < gun.StockMax();
        }

        public static bool IsPoor(this GunComponent.Component gun, out List<GunInfo> emptyList)
        {
            emptyList = null;
            bool isEmpty = false;
            foreach (var kvp in gun.GunsDic) {
                if (kvp.Value.IsPoor() == false)
                    continue;

                emptyList = emptyList ?? new List<GunInfo>();
                emptyList.Add(kvp.Value);
                isEmpty |= true;
            }

            return isEmpty;
        }

        public static void AddBullets(ref this GunInfo gun, int num)
        {
            gun.StockBullets = Mathf.Clamp(gun.StockBullets + num, 0, gun.StockMax());
        }

        public static int StockMax (this GunInfo info)
        {
            var gun = GunDictionary.GetGunSettings(info.GunTypeId);
            if (gun == null)
                return 0;

            return gun.Stock;
        }

        public static float AttackRange (this GunInfo info)
        {
            var gun = GunDictionary.GetGunSettings(info.GunTypeId);
            if (gun == null)
                return 0;

            return gun.AtkRange;
        }

        public static float AttackAngle (this GunInfo info)
        {
            var gun = GunDictionary.GetGunSettings(info.GunTypeId);
            if (gun == null)
                return 0;

            return gun.AtkRange;
        }

        public static float LifeTime (this BulletFireInfo bullet)
        {
            var gun = GunDictionary.GetGunSettings(bullet.GunId);
            if (gun == null)
                return 0;

            return gun.BulletLifeTime;
        }


        public static void ChangeState(this List<UnitContainer> containers, int index, ContainerState state)
        {
            if (containers == null || containers.Count <= index)
                return;

            var c = containers[index];
            c.State = state;
            containers[index] = c;
        }

        public static Vector3 GetVector3(this BoidVector boidVector, float range)
        {
            var vec = boidVector.Vector.ToUnityVector();
            return vec.normalized * ( range + vec.magnitude);
        }

        public static float SqrtBoidRadius(this BoidVector boidVector)
        {
            var radius = boidVector.BoidRadius;
            return radius * radius;
        }

        public static Coordinates GetGrounded(this Coordinates coords, Vector3 origin, float height_buffer)
        {
            var pos = PhysicsUtils.GetGroundPosition((float)coords.X, origin.y, (float)coords.Z);
            pos += Vector3.up * height_buffer;
            return pos.ToWorldPosition(origin).ToCoordinates();
        }

        public static bool IsDominationTarget(this UnitBaseInfo unitInfo, UnitSide selfSide)
        {
            if (unitInfo.Type != UnitType.Stronghold)
                return false;

            if (unitInfo.Side == selfSide)
                return false;

            return unitInfo.Side == UnitSide.None ||
                   unitInfo.State == UnitState.Dead;
        }

        public static bool IsDominationTarget(this UnitInfo unitInfo, UnitSide selfSide)
        {
            if (unitInfo.type != UnitType.Stronghold)
                return false;

            if (unitInfo.side == selfSide)
                return false;

            return unitInfo.side == UnitSide.None ||
                   unitInfo.state == UnitState.Dead;
        }

        public static Vector3 GetTargetPosition(this BaseUnitSight.Component sight, Vector3 origin, Vector3 current)
        {
            var tgt = sight.TargetPosition.ToWorkerPosition(origin);
            var diff = current - tgt;
            return tgt + diff.normalized * sight.TargetSize;
        }

        public static bool IsValid(this HexBaseInfo hexInfo)
        {
            return hexInfo.HexIndex < uint.MaxValue;
        }

        public static bool CornerEquals(this FrontLineInfo info, FrontLineInfo other)
        {
            return info.LeftCorner == other.LeftCorner && info.RightCorner == other.RightCorner;
        }

        public static bool IsValid(this FrontLineInfo info)
        {
            return info.LeftCorner.Equals(info.RightCorner) == false;
        }

        public static bool IsValid(this UnitBaseInfo info)
        {
            return info.UnitId.IsValid();
        }

        public static Vector3 GetOnLinePosition(this FrontLineInfo info, Vector3 origin, Vector3 current, float fowardBuffer = 0.0f)
        {
            var left = info.LeftCorner.ToUnityVector() + origin;
            var right = info.RightCorner.ToUnityVector() + origin;

            var line = right - left;
            var foward = Vector3.Cross(line, Vector3.up).normalized;

            if (fowardBuffer != 0) {
                left += foward * fowardBuffer;
                right += foward * fowardBuffer;
            }

            var center = (right + left) / 2;
            var diff = current - left;

            var radius = line.magnitude / 2;

            // 中心誘導は廃止
            //if (Vector3.Dot(Vector3.Cross(line.normalized, diff), Vector3.up) < -radius / 4 ||
            //    (current - center).sqrMagnitude > radius * radius) {
            //
            //    return -foward * radius / 2 + center;
            //}

            var dot = Vector3.Dot(diff, line.normalized);
            if (dot <= 0)
                return left;
            else if (dot >= line.magnitude)
                return right;

            return line.normalized * dot + left;
        }

        public static bool IsDominatable(this HexAttribute attribute)
        {
            switch (attribute)
            {
                case HexAttribute.Field:
                case HexAttribute.ForwardBase:
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsTargetable(this HexAttribute attribute)
        {
            switch (attribute)
            {
                case HexAttribute.NotBelong:
                    return false;

                default:
                    return true;
            }
        }
    }

    public static class IntervalCheckerInitializer
    {
        public static IntervalChecker InitializedChecker(int period, bool setChecked = false)
        {
            period = period > 0 ? period: 1;
            return InitializedChecker(1.0f/period, setChecked);
        }

        public static IntervalChecker InitializedChecker(float inter, bool setChecked = false)
        {
            return new IntervalChecker(inter, setChecked ? float.MinValue: 0, 0, -1);
        }
    }
}

