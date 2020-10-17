using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Gun Config/GunSettings", order = 1)]
    public class GunSettings : ScriptableObject
    {
        [SerializeField] uint typeId = 1;
        public uint TypeId => typeId;
        [SerializeField] int stock = 30;
        public int Stock => stock;
        [SerializeField] float atkRange = 15.0f;
        public float AtkRange => atkRange;
        [SerializeField] float atkAngle = 1.0f;
        public float AtkAngle => atkAngle;
        [SerializeField] float inter = 0.5f;
        public float Inter => inter;
        [SerializeField] uint bulletTypeId = 0;
        public uint BulletTypeId => bulletTypeId;
        [SerializeField] float bulletSpeed = 15.0f;
        public float BulletSpeed => bulletSpeed;
        [SerializeField] PosturePoint attached = PosturePoint.Root;
        public PosturePoint Attached => attached;

        public float BulletLifeTime
        {
            get
            {
                return bulletSpeed > 0 ? atkRange / bulletSpeed: 0.0f;
            }
        }

        public GunInfo GetGunInfo(ulong uid, int bone)
        {
            var info = new GunInfo
            {
                GunId = uid,
                GunTypeId = typeId,
                StockBullets = stock,
                StockMax = stock,
                AttackRange = atkRange,
                AttackAngle = atkAngle,
                Interval = IntervalCheckerInitializer.InitializedChecker(inter),
                AttachedBone = bone,
            };

            return info;
        }
    }
}
