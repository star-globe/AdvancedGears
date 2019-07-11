using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(menuName = "AdvancedGears/Gun Config/GunSettings", order = 1)]
    public class GunSettings : ScriptableObject
    {
        [SerializeField] uint typeId = 1;
        [SerializeField] int stock = 30;
        [SerializeField] float atkRange = 15.0f;
        [SerializeField] float atkAngle = 1.0f;
        [SerializeField] float inter = 0.5f;
        [SerializeField] PosturePoint attached = PosturePoint.Root;

        public uint TypeId => typeId;
        public int Stock => stock;
        public float Inter => inter;
        public float AtkRange => atkRange;
        public float AtkAngle => atkAngle;
        public PosturePoint Attached => attached;

        public GunInfo GetGunInfo(ulong uid)
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
                AttachedPosture = attached,
            };

            return info;
        }
    }
}
