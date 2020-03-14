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
    }
}
