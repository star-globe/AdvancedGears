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
        [SerializeField] private float drag;
        [SerializeField] private float angularDrag;
        [SerializeField] private Vector3 centerOfMass;
        [SerializeField] private bool useGravity;
        [SerializeField] private RigidbodyConstraints[] constraints;

        public void SetRigid(Rigidbody rigid)
        {
            rigid.mass = mass;

            rigid.drag = drag;
            rigid.angularDrag = angularDrag;

            rigid.useGravity = useGravity;

            rigid.centerOfMass = centerOfMass;

            var cst = RigidbodyConstraints.None;
            if (constraints != null) {
                foreach (var c in constraints)
                    cst |= c;
            }

            rigid.constraints = cst;
        }
    }
}
