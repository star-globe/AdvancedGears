using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/BaseUnit Config/InitSettings", order = 1)]
    public class BaseUnitInitSettings : ScriptableObject
    {
        [SerializeField] float speed = 1.0f;
        [SerializeField] float rotSpeed = 3.0f;
        [SerializeField] float sightRange = 30.0f;
        [SerializeField] int maxHp = 10;
        [SerializeField] int defense = 10;
        [SerializeField] int maxFuel = 50;
        [SerializeField] float consumeRate = 1.0f;

        public float Speed => speed;
        public float RotSpeed => rotSpeed;
        public float SightRange => sightRange;
        public int MaxHp => maxHp;
        public int Defense => defense;
        public int MaxFuel => maxFuel;
        public float ConsumeRate => consumeRate;
    }
}
