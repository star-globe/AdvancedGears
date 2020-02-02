using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/FixedParam Config/Order Dictionary", order = 0)]
    public class OrderDictionary : DictionarySettings
    {
        public static OrderDictionary Instance { private get; set; }

        [SerializeField] private MaxRankSettings[] numberSettings;

        Dictionary<OrderType, MaxRankSettings> dic = null;
        Dictionary<OrderType, MaxRankSettings> Dic
        {
            get
            {
                if (dic == null) {
                    dic = new Dictionary<OrderType, MaxRankSettings>();

                    foreach (var set in numberSettings)
                    {
                        if (dic.ContainsKey(set.order))
                            continue;

                       dic.Add(set.order, set);
                    }
                }

                return dic;
            }
        }

        public override void Initialize()
        {
            Instance = this;
        }

        public static uint GetMaxRank(OrderType orderType, uint rank)
        {
			if (Instance.Dic.TryGetValue(orderType, out var set) == false)
				return 0;

			return set.GetMaxRank(rank);
        }

        [Serializable]
        public class MaxRankSettings
		{
            public OrderType order;
            public int perRank;
			public int fix;

			public uint GetMaxRank(uint rank)
            {
				return (uint)(rank * perRank + fix);
            }
        }
    }
}
