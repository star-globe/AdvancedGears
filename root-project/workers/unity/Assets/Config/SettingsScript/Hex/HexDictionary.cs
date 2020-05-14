using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AdvancedGears
{
    [CreateAssetMenu(menuName = "AdvancedGears/Strategy Config/Hex Dictionary", order = 0)]
    public class HexDictionary : DictionarySettings
    {
        public static HexDictionary Instance { get; private set; }

        [SerializeField] private HexSettings[] hexesList;
        readonly Dictionary<int, HexSettings> hexesDic = new Dictionary<int, HexSettings>();

        [SerializeField] private float edgeLength;
        public float EdgeLength => edgeLength;
        public static float EdgeLength => Instance.EdgeLength;

        public override void Initialize()
        {
            Instance = this;

            foreach (var h in hexesList)
                hexesDic[h.Id] = f;
        }

        public static HexSettings Get(int hex_type_id)
        {
            if (Instance == null)
            {
                Debug.LogError("The Hex Dictionary has not been set.");
                return null;
            }

            HexSettings settings = null;
            Instance.hexesDic.TryGetValue(hex_type_id, out settings);

            return settings;
        }
    }
}
