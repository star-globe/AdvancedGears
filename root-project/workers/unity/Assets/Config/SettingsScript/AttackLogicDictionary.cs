using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/AttackLogicDictionary", order = 0)]
    public class AttackLogicDictionary : DictionarySettings
    {
		[Serializable]
		class OrderRate
		{
            public OrderType order;
			public float rangeRate;
			public float speedRate;
		}

		[SerializeField]
		OrderRate[] orderRates;

        [SerializeField]
        float judgeRate = 1.5f;
        public static float JudgeRate => Instance.judgeRate;

        [SerializeField]
        float boidReduceRate = 0.8f;
        public static float BoidReduceRate => Instance.boidReduceRate;

        [SerializeField]
        float boidPotentialMinimum = 1.0f / 1000;
        public static float BoidPotentialMinimum => Instance.boidPotentialMinimum;

        [SerializeField]
        float boidRadiusMinimum = 0.1f;

        [SerializeField]
        int underSoldiers = 5;
        public static int UnderSoldiers => Instance.underSoldiers;

        [SerializeField]
        int underCommanders = 3;
        public static int UnderCommanders => Instance.underCommanders;

        public static AttackLogicDictionary Instance { private get; set; }

        readonly Dictionary<OrderType, OrderRate> orderDic = new Dictionary<OrderType, OrderRate>();
        private static Dictionary<OrderType, OrderRate> OrderDic => Instance.orderDic;

        public override void Initialize()
        {
            Instance = this;

            foreach (var o in orderRates)
                orderDic[o.order] = o;
        }

        public static float GetOrderRange(OrderType order, float baseRange)
        {
			if (OrderDic.TryGetValue(order, out var rates))
				baseRange = rates.rangeRate * baseRange;

			return baseRange;
        }

        public static float GetOrderSpeed(OrderType order, float baseSpeed)
        {
            if (OrderDic.TryGetValue(order, out var rates))
                baseSpeed = rates.rangeRate * baseSpeed;

            return baseSpeed;
        }

        public static float ReduceBoidPotential(float potential, float time)
        {
            potential *= Mathf.Pow(BoidReduceRate, time);
            return potential < BoidPotentialMinimum ? 0.0f: potential;
        }

        public static float BoidPotential(uint rank, float length, float radius)
        {
            length = Mathf.Max(length, Instance.boidRadiusMinimum);
            var rate = length / radius;
            return rank / (rate * rate);
        }

        public static float RankScaled(float range, uint rank)
        {
            if (rank < 1)
                return range;
            else
                return range * Mathf.Pow(UnderCommanders, rank);
        }
    }
}
