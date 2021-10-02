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
        [SerializeField] float lifeTimeRate = 1.2f;
        public float LifeTimeRate => lifeTimeRate;

        public float BulletLifeTime
        {
            get
            {
                if (bulletSpeed <= 0)
                    return 0;

                return lifeTimeRate * atkRange / bulletSpeed;
            }
        }

        public float VanishRange
        {
            get { return LifeTimeRate * AtkRange; }
        }

        public GunInfo GetGunInfo(ulong uid, int bone)
        {
            var info = new GunInfo
            {
                GunId = uid,
                GunTypeId = typeId,
                StockBullets = stock,
                Interval = IntervalCheckerInitializer.InitializedChecker(inter),
                AttachedBone = bone,
            };

            return info;
        }
    }
}
