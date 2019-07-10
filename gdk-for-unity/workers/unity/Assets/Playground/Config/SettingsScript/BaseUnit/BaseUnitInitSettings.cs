using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playground
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/InitSettings", order = 1)]
    public class BaseUnitInitSettings : ScriptableObject
    {
        [SerializeField] float speed = 1.0f;
        [SerializeField] float rot = 3.0f;
        [SerializeField] float inter = 0.5f;
        [SerializeField] float sightRange = 30.0f;
        [SerializeField] float atkRange = 15.0f;
        [SerializeField] float atkAngle = 1.0f;
        [SerializeField] float angleSpeed = 90.0f;
        [SerializeField] int maxHp = 10;
        [SerializeField] int defense = 10;
        [SerializeField] int maxFuel = 50;
        [SerializeField] float consumeRate = 1.0f;
        [SerializeField] uint[] gunIds = new uint[] { 1 };

        public float Speed => speed;
        public float Rot => rot;
        public float Inter => inter;
        public float SightRange => sightRange;
        public float AtkRange => atkRange;
        public float AtkAngle => atkAngle;
        public float AngleSpeed => angleSpeed;
        public int MaxHp => maxHp;
        public int Defense => defense;
        public int MaxFuel => maxFuel;
        public float ConsumeRate => consumeRate;
        public uint[] GunIds => gunIds;
    }
}
