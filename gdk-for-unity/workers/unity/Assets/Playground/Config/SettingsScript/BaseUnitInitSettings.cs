using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/Init Settings", order = 2)]
    public class BaseUnitInitSettings : ScriptableObject
    {
        [SerializeField] float speed = 1.0f;
        [SerializeField] float rot = 0.3f;
        [SerializeField] float inter = 0.5f;
        [SerializeField] float sightRange = 10.0f;
        [SerializeField] float atkRange = 9.0f;
        [SerializeField] float atkAngle = 5.0f;
        [SerializeField] float angleSpeed = 5.0f;

        [SerializeField] int maxHealth = 10;
        [SerializeField] int defense = 10;

        public float Speed => speed;
        public float Rot => rot;
        public float Inter => inter;
        public float SightRange => sightRange;
        public float AtkRange => atkRange;
        public float AtkAngle => atkAngle;
        public float AngleSpeed => angleSpeed;

        public int MaxHealth => maxHealth;
        public int Defense => defense;
    }
}

