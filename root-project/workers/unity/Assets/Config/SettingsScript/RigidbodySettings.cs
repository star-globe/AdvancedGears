using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/Rigidbody Settings", order = 1)]
    public class RigidbodySettings : ScriptableObject
    {
        [SerializeField] private float mass;
        //[SerializeField] private GameObject impactEffect;
        //[SerializeField] private GameObject muzzleFlashEffect;
        //[SerializeField] private GameObject bulletLineRenderer;
        //[SerializeField] private Color shotColour;
        //[SerializeField] private float shotDamage;
        //[SerializeField] private float shotRange;
        //[SerializeField] private float bulletRenderLength;
        //[SerializeField] private float shotRenderTime;
        //[SerializeField] private byte alignment;
        [SerializeField] private bool isBuilding;

        public void SetRigid(Rigidbody rigid)
        {
            rigid.mass = mass;

            if (isBuilding)
                rigid.constraints = RigidbodyConstraints.FreezeAll;

            rigid.useGravity = true;
        }
    }
}
