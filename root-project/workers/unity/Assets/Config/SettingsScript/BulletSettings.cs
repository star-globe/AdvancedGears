using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Bullet Config/Bullet Settings", order = 1)]
    public class BulletSettings : ScriptableObject
    {
        [SerializeField] uint typeId = 1;
        public uint TypeId => typeId;
        [SerializeField] private GameObject bulletModel;
        public GameObject BulletModel => bulletModel;
        [SerializeField] private BulletBaseType baseType;
        public BulletBaseType BulletBaseType => baseType;
        [SerializeField] private int option;
        public int Option => option;
        [SerializeField] private GameObject impactEffect;
        [SerializeField] private GameObject muzzleFlashEffect;
        [SerializeField] private GameObject bulletLineRenderer;
        [SerializeField] private Color shotColour;
        [SerializeField] private float shotDamage;
        [SerializeField] private float shotRange;
        [SerializeField] private float bulletRenderLength;
        [SerializeField] private float shotRenderTime;
        [SerializeField] private byte alignment;

        public GameObject ImpactEffect => impactEffect;

        public GameObject MuzzleFlashEffect => muzzleFlashEffect;

        public GameObject BulletLineRenderer => bulletLineRenderer;

        public Color ShotColour => shotColour;

        public float ShotDamage => shotDamage;

        public float BulletRenderLength => bulletRenderLength;

        public float ShotRange => shotRange;

        public float ShotRenderTime => shotRenderTime;

        //public float ShotCooldown => shotCooldown;
        //
        //public bool IsAutomatic => isAutomatic;
        //
        //public RecoilSettings HipRecoil => hipRecoil;
        //
        //public RecoilSettings AimRecoil => aimRecoil;
        //
        //public float AimFov => aimFov;
        //
        //public float FovChangeTime => fovChangeTime;
        //
        //public float InaccuracyFromHip => inaccuracyFromHip;
        //
        //public float InaccuracyWhileAiming => inaccuracyWhileAiming;
        //
        //public float AimPitchSpeed => aimPitchSpeed;
        //
        //public float AimYawSpeed => aimYawSpeed;

        //[System.Serializable]
        //public class RecoilSettings
        //{
        //    public FirstPersonRecoilSettings FirstPersonRecoil;
        //    public ThirdPersonRecoilSettings ThirdPersonRecoil;
        //}

        /*
        [System.Serializable]
        public class FirstPersonRecoilSettings
        {
            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MinXVariance = 0;

            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MaxXVariance = 0;

            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MinYVariance = 0;

            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MaxYVariance = 0;

            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MinZVariance = 0;

            [Tooltip(
                "When recoiling, the gun will be moved by a Vector3 randomly between the Min and Max variance in each axis.")]
            public float MaxZVariance = 0;

            [Tooltip("The offset is increased each update using a velocity damped by this value.")]
            public float VelocityDamper = 0;

            [Tooltip("The actual offset (after the velocity is applied) is damped by this value (to return to zero).")]
            public float OffsetDamper = 0;
        }

        [System.Serializable]
        public class ThirdPersonRecoilSettings
        {
            [Tooltip("The speed/strenth at which the gun recoils back.")]
            public float RecoilStrength = 0;

            [Tooltip("The time for which the gun recoils back.")]
            public float RecoilTime = 0.1f;

            [Tooltip("The time taken for the gun to return to its original position.")]
            public float ResetTime = 0.2f;
        }
        */

        private void OnValidate()
        {
            //shotCooldown = rateOfFire > 0 ? 1f / rateOfFire : 0;

            if (bulletModel != null)
            {
                ValidateGunPrefab(bulletModel);
            }

            if (muzzleFlashEffect != null)
            {
                ValidateMuzzleFlashPrefab(muzzleFlashEffect);
            }
        }

        private void ValidateGunPrefab(GameObject prefab)
        {
            //var gunHandle = prefab.GetComponent<GunHandle>();
            //if (gunHandle == null)
            //{
            //    Debug.LogWarningFormat("The Gun prefab '{0}' is missing a Handle", prefab.name);
            //    return;
            //}
            //
            //if (gunHandle.Barrel == null)
            //{
            //    Debug.LogWarningFormat("The Gun prefab '{0}' is missing a Barrel.", gunHandle.name);
            //}
            //
            //if (gunHandle.Grip == null)
            //{
            //    Debug.LogWarningFormat("The Gun prefab '{0}' is missing a Grip.", gunHandle.name);
            //}
            //
            //if (gunHandle.Scope == null)
            //{
            //    Debug.LogWarningFormat("The Gun prefab '{0}' is missing a Scope.", gunHandle.name);
            //}
        }

        private void ValidateMuzzleFlashPrefab(GameObject prefab)
        {
            if (prefab.GetComponentInChildren<ParticleSystem>() == null)
            {
                Debug.LogWarningFormat("The Muzzle Flash Effect '{0}' has no Particle Systems.", prefab.name);
            }
        }
    }
}
